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

namespace NaturalLanguageProcessing.ViterbiTagger;

internal static class Program
{
    private static readonly TagCollection s_tags = new TagCollection();
    private static readonly HashSet<string> s_words = new HashSet<string>();

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

        //Console.WriteLine("Words: {0:n0}\nTags: {1:n0}\nEmissions: {2:n0}\nTransitions: {3:n0}\nTotal emissions: {4:n0}\nTotal transitions: {5:n0}",
        //    words.Count,
        //    tags.Count,
        //    tags.SelectMany(x => x.Emissions).Count(),
        //    tags.SelectMany(x => x.Transitions).Count(),
        //    tags.Sum(x => x.TotalEmissions),
        //    tags.Sum(x => x.TotalTransitions));
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
        Tag? previous = null;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] segments = line.Split();
            string word = segments[0];
            string tag = segments[1];

            s_words.Add(word);

            if (!s_tags.TryGetValue(tag, out Tag? current))
            {
                current = new Tag(tag);

                s_tags.Add(current);
            }

            current.AddEmission(word);

            if (previous != null)
            {
                previous.AddTransition(current);
            }

            previous = current;
        }
    }

    private static void TagFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);

        string? line;

        while ((line = reader.ReadLine()) != null)
        {

        }
    }
}
