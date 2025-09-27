// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

// References:
//  - https://en.wikipedia.org/wiki/Viterbi_algorithm#Pseudocode
//  - https://medium.com/data-science-in-your-pocket/pos-tagging-using-hidden-markov-models-hmm-viterbi-algorithm-in-nlp-mathematics-explained-d43ca89347c4

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NaturalLanguageProcessing.ViterbiTagger;

internal static class Program
{
    private const string SentenceStart = "Begin_Sent";
    private const string SentenceEnd = "End_Sent";
    //private const string UnknownWord = "Unknown_Word";
    private const int SmoothingK = 1;
    private const int UnknownK = 1;
    private static readonly Dictionary<string, int> words =
        new Dictionary<string, int>();
    private static readonly Dictionary<string, int> tags =
        new Dictionary<string, int>();
    private static readonly Dictionary<(string Word, string Tag), double> likelihood =
        new Dictionary<(string Word, string Tag), double>();
    private static readonly Dictionary<(string Source, string Target), double> transition =
        new Dictionary<(string Source, string Target), double>();

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: {0} <pos_file> <words_file>", Process.GetCurrentProcess().ProcessName);

            return;
        }

        string posFileName = args[0];
        string wordsFileName = args[1];

        CheckFile(posFileName);
        CheckFile(wordsFileName);

        Stopwatch watch = Stopwatch.StartNew();

        ReadPosFile(posFileName);
        TagFile(wordsFileName);
        watch.Stop();
        Console.WriteLine("{0:n0} ms elapsed.", watch.ElapsedMilliseconds);
    }

    private static void CheckFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            Console.WriteLine("File does not exist: \"{0}\".", fileName);
            Environment.Exit(0);
        }
    }

    private static void ReadPosFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);

        string? line;
        string previous = SentenceStart;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (previous == SentenceStart)
            {
                tags[SentenceStart] = tags.GetValueOrDefault(SentenceStart) + 1;
            }

            string? word;
            string tag;

            if (string.IsNullOrWhiteSpace(line))
            {
                word = null;
                tag = SentenceEnd;
            }
            else
            {
                string[] segments = line.Split(
                    new string[] { "\t", " " },
                    StringSplitOptions.TrimEntries);

                word = segments[0].ToUpperInvariant();
                tag = segments[1].ToUpperInvariant();
            }

            tags[tag] = tags.GetValueOrDefault(tag) + 1;
            transition[(previous, tag)] = transition.GetValueOrDefault((previous, tag)) + 1;

            if (word != null)
            {
                words[word] = words.GetValueOrDefault(word) + 1;
                likelihood[(word, tag)] = likelihood.GetValueOrDefault((word, tag)) + 1;
            }

            if (tag == SentenceEnd)
            {
                previous = SentenceStart;
            }
            else
            {
                previous = tag;
            }
        }

        foreach (string word in words
            .Where(x => x.Value <= UnknownK)
            .Select(x => x.Key)
            .ToList())
        {
            words.Remove(word);

            string guess = TagUnknown(word);

            words[guess] = words.GetValueOrDefault(guess) + 1;

            foreach (string tag in tags.Keys)
            {
                if (likelihood.TryGetValue((word, tag), out double prior))
                {
                    likelihood.Remove((word, tag));

                    likelihood[(guess, tag)] =
                        likelihood.GetValueOrDefault((guess, tag))
                        + prior;
                }
            }
        }

        foreach (KeyValuePair<(string Source, string Target), double> entry in transition)
        {
            transition[entry.Key] = (entry.Value + SmoothingK) / (tags[entry.Key.Source] + SmoothingK);
        }

        foreach (KeyValuePair<(string Word, string Tag), double> entry in likelihood)
        {
            likelihood[entry.Key] = (entry.Value + SmoothingK) / (tags[entry.Key.Tag] + SmoothingK);
        }
    }

    private static string TagUnknown(string word)
    {
        return "Unknown_Word";
    }

    private static void TagFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);
        using StreamWriter writer = File.CreateText("submission.pos");

        List<string> sentence = new List<string>();
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                RealizeSentence(writer, sentence);
            }
            else
            {
                sentence.Add(line);
            }
        }

        RealizeSentence(writer, sentence);
    }

    private static void RealizeSentence(StreamWriter writer, List<string> sentence)
    {
        if (sentence.Count == 0)
        {
            return;
        }

        string[] tags = TagSentence(sentence);

        for (int i = 0; i < sentence.Count; i++)
        {
            writer.WriteLine("{0}\t{1}", sentence[i], tags[i]);
        }

        writer.WriteLine();
        sentence.Clear();
    }

    private static string[] TagSentence(List<string> sentence)
    {
        string[] states = new string[tags.Count];

        states[0] = SentenceStart;

        int j = 1;

        foreach (string tag in tags.Keys)
        {
            if (tag != SentenceStart && tag != SentenceEnd)
            {
                states[j] = tag;
                j++;
            }
        }

        states[states.Length - 1] = SentenceEnd;

        double[,] viterbi = new double[sentence.Count + 1, tags.Count];

        viterbi[0, 0] = 1;

        int[,] backpointer = new int[sentence.Count + 1, tags.Count];
        int argMax;
        double max;

        for (int t = 1; t <= sentence.Count; t++)
        {
            string word = sentence[t - 1].ToUpperInvariant();

            for (int q = 0; q < states.Length; q++)
            {
                argMax = -1;
                max = -1;

                for (int p = 0; p < states.Length; p++)
                {
                    double current =
                        viterbi[t - 1, p]
                        * transition.GetValueOrDefault((states[p], states[q]))
                        * GetLikelihood(word, states[q]);

                    if (current > max)
                    {
                        argMax = p;
                        max = current;
                    }
                }

                if (argMax != -1)
                {
                    viterbi[t, q] = max;
                    backpointer[t, q] = argMax;
                }
            }
        }

        argMax = -1;
        max = -1;

        for (int p = 0; p < states.Length; p++)
        {
            double current =
                transition.GetValueOrDefault((states[p], SentenceEnd))
                * viterbi[sentence.Count, p];

            if (current > max)
            {
                argMax = p;
                max = current;
            }
        }

        if (argMax == -1)
        {
            return Array.Empty<string>();
        }

        string[] results = new string[sentence.Count];

        results[results.Length - 1] = states[argMax];

        for (int i = backpointer.GetLength(0) - 1; i > 1; i--)
        {
            argMax = backpointer[i, argMax];
            results[i - 2] = states[argMax];
        }

        return results;
    }

    private static double GetLikelihood(string word, string tag)
    {
        if (words.ContainsKey(word))
        {
            return likelihood.GetValueOrDefault((word, tag));
        }

        string guess = TagUnknown(word);

        if (words.ContainsKey(guess))
        {
            return likelihood.GetValueOrDefault((guess, tag));
        }

        return 1d / 1000;
    }
}
