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

    private static readonly Dictionary<string, int> tags = new Dictionary<string, int>()
    {
        { SentenceStart, 0 },
        { SentenceEnd, 0 }
    };
    private static readonly Dictionary<(string Tag, string Word), double> likelihood =
        new Dictionary<(string Tag, string Word), double>();
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
        ReadPosFile(posFileName);

        Console.WriteLine("Words: {0:n0}\nTags: {1:n0}\nEmissions: {2:n0}\nTransitions: {3:n0}\nTotal emissions: {4:n0}\nTotal transitions: {5:n0}",
            -1,
            -1,
            tags.Sum(x => x.Value),
            -1,
            likelihood.Sum(x => x.Value),
            transition.Sum(x => x.Value));

        foreach ((string Tag, string Word) key in likelihood.Keys)
        {
            likelihood[key] /= tags[key.Tag];
        }

        foreach ((string Source, string Target) key in transition.Keys)
        {
            transition[key] /= tags[key.Source];
        }

        tags.Remove(SentenceStart);
        tags.Remove(SentenceEnd);
        TagFile(wordsFileName);

        //Console.WriteLine("Transitions (top 100):");

        //foreach (KeyValuePair<(string Source, string Target), double> entry in transition
        //    .OrderByDescending(x => x.Value)
        //    .Take(100))
        //{
        //    Console.WriteLine("{0}: {1} [{2:p4}]", entry.Key.Source, entry.Key.Target, entry.Value);
        //}


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

        string? line;
        List<string> current = new List<string>();
        List<List<string>> sentences = new List<List<string>>();

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                RealizeSentence(sentences, current);
            }
            else
            {
                current.Add(line);
            }
        }

        RealizeSentence(sentences, current);
    }

    private static void RealizeSentence(List<List<string>> sentences, List<string> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        foreach ((string tag, string word) in current.Zip(TagSentence(current)))
        {
            Console.WriteLine("{0} {1}", word, tag);
        }

        current.Clear();
    }

    private static IEnumerable<string> TagSentence(List<string> sentence)
    {
        int n = tags.Count;
        int f = n + 1;
        string[] q = new string[n + 2];

        q[0] = SentenceStart;

        tags.Keys.CopyTo(q, index: 1);

        q[f] = SentenceEnd;

        double[,] a = new double[n + 2, n + 2];

        for (int i = 0; i < f; i++)
        {
            for (int j = 0; j < f; j++)
            {
                a[i, j] = transition.GetValueOrDefault((q[i], q[j]));
            }
        }

        int t = sentence.Count;
        double[,] b = new double[n + 1, t];

        for (int i = 0; i <= n; i++)
        {
            for (int u = 0; u < t; u++)
            {
                b[i, u] = likelihood.GetValueOrDefault((q[i], sentence[u].ToUpperInvariant()));
            }
        }

        double[,] viterbi = new double[n + 2, t];
        int[,] backpointer = new int[n + 2, t];

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

        viterbi[f, t - 1] = viterbi[1, t - 1] * a[1, f];
        backpointer[f, t - 1] = 1;

        for (int i = 2; i <= n; i++)
        {
            double next = viterbi[i, t - 1] * a[i, f];

            if (next > viterbi[f, t - 1])
            {
                viterbi[f, t - 1] = next;
                backpointer[f, t - 1] = i;
            }
        }

        int[] results = new int[t];

        results[t - 1] = backpointer[f, t - 1];

        for (int u = t - 2; u >= 0; u--)
        {
            results[u] = backpointer[results[u + 1], u + 1];
        }

        foreach (int i in results)
        {
            yield return q[i];
        }
    }
}
