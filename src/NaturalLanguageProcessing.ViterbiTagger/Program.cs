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
    private const int SmoothingK = 1;

    private static readonly Dictionary<string, int> tags = new Dictionary<string, int>()
    {
        { SentenceStart, 0 },
        { SentenceEnd, 0 }
    };
    private static readonly Dictionary<(string Tag, string Word), int> likelihood =
        new Dictionary<(string Tag, string Word), int>();
    private static readonly Dictionary<(string Source, string Target), int> transition =
        new Dictionary<(string Source, string Target), int>();

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
        ReadPosFile(posFileName);
        TagFile(wordsFileName);
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
        string previousTag = SentenceStart;

        while ((line = reader.ReadLine()) != null)
        {
            if (previousTag == SentenceStart)
            {
                tags[SentenceStart]++;
            }

            (string tag, string? word) = ParseLine(line);
            (string, string) key;

            if (word != null)
            {
                key = (tag, word);

                likelihood[key] = likelihood.GetValueOrDefault(key) + 1;
            }

            tags[tag] = tags.GetValueOrDefault(tag) + 1;

            key = (previousTag, tag);

            transition[key] = transition.GetValueOrDefault(key) + 1;

            if (tag == SentenceEnd)
            {
                previousTag = SentenceStart;
            }
            else
            {
                previousTag = tag;
            }
        }
    }

    private static (string tag, string? word) ParseLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return (SentenceEnd, null);
        }

        string[] segments = line.Split();
        string word = segments[0].ToUpperInvariant();
        string tag = segments[1].ToUpperInvariant();

        return (tag, word);
    }

    private static void TagFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);
        using StreamWriter writer = File.CreateText("submission.pos");

        string? line;
        List<string> current = new List<string>();
        List<List<string>> sentences = new List<List<string>>();

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                RealizeSentence(writer, sentences, current);
            }
            else
            {
                current.Add(line);
            }
        }

        RealizeSentence(writer, sentences, current);
    }

    private static void RealizeSentence(StreamWriter writer, List<List<string>> sentences, List<string> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        foreach ((string tag, string word) in TagSentence(current).Zip(current))
        {
            writer.WriteLine("{0}\t{1}", word, tag);
        }

        writer.WriteLine();
        current.Clear();
    }

    private static IEnumerable<string> TagSentence(List<string> sentence)
    {
        int sentenceStart = tags[SentenceStart];
        int sentenceEnd = tags[SentenceEnd];

        tags.Remove(SentenceStart);
        tags.Remove(SentenceEnd);

        int n = tags.Count;
        int f = n + 1;
        string[] q = new string[n + 2];

        q[0] = SentenceStart;

        tags.Keys.CopyTo(q, index: 1);

        tags[SentenceStart] = sentenceStart;
        tags[SentenceEnd] = sentenceEnd;
        q[f] = SentenceEnd;

        double[,] a = new double[n + 2, n + 2];

        for (int i = 0; i < f; i++)
        {
            for (int j = 0; j < f; j++)
            {
                string source = q[i];

                a[i, j] = (double)transition.GetValueOrDefault((source, q[j])) / tags[source];
            }
        }

        int t = sentence.Count;
        double[,] b = new double[n + 1, t];

        for (int i = 0; i <= n; i++)
        {
            for (int u = 0; u < t; u++)
            {
                (string Tag, string Word) key = (q[i], sentence[u].ToUpperInvariant());

                if (likelihood.TryGetValue(key, out int count))
                {
                    b[i, u] = (double)(count + SmoothingK) / (tags[key.Tag] + SmoothingK);
                }
                else
                {
                    b[i, u] = 0.001;
                }
            }
        }

        double[,] viterbi = new double[n + 1, t];
        int[,] backpointer = new int[n + 1, t];

        for (int i = 1; i <= n; i++)
        {
            viterbi[i, 0] = a[0, i] * b[i, 0];
            backpointer[i, 0] = 0;
        }

        for (int u = 1; u < t; u++)
        {
            for (int i = 1; i <= n; i++)
            {
                viterbi[i, u] = viterbi[1, u - 1] * a[1, i] * b[i, u];
                backpointer[i, u] = 1;

                for (int j = 2; j <= n; j++)
                {
                    double next = viterbi[j, u - 1] * a[j, i] * b[i, u];

                    if (next > viterbi[i, u])
                    {
                        viterbi[i, u] = next;
                        backpointer[i, u] = j;
                    }
                }
            }
        }

        double maxViterbi = viterbi[1, t - 1] * a[1, f];
        int argMaxBackpointer = 1;

        for (int i = 2; i <= n; i++)
        {
            double next = viterbi[i, t - 1] * a[i, f];

            if (next > maxViterbi)
            {
                maxViterbi = next;
                argMaxBackpointer = i;
            }
        }

        int[] results = new int[t];

        results[t - 1] = argMaxBackpointer;

        for (int u = t - 2; u >= 0; u--)
        {
            results[u] = backpointer[results[u + 1], u + 1];
        }

        return results.Select(i => q[i]);
    }
}
