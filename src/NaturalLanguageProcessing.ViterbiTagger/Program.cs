// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NaturalLanguageProcessing.ViterbiTagger;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: {0} <file>", Process.GetCurrentProcess().ProcessName);

            return;
        }

        string fileName = args[0];

        if (!File.Exists(fileName))
        {
            Console.WriteLine("File does not exist: \"{0}\".", fileName);

            return;
        }

        using StreamReader reader = File.OpenText(fileName);

        string? line;
        Tag? previous = null;
        HashSet<string> words = new HashSet<string>();
        TagCollection tags = new TagCollection();

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] segments = line.Split();
            string word = segments[0];
            string tag = segments[1];

            words.Add(word);

            if (!tags.TryGetValue(tag, out Tag? current))
            {
                current = new Tag(tag);

                tags.Add(current);
            }

            current.AddEmission(word);

            if (previous != null)
            {
                previous.AddTransition(current);
            }

            previous = current;
        }

        Console.WriteLine("Words: {0:n0}\nTags: {1:n0}\nEmissions: {2:n0}\nTransitions: {3:n0}\nTotal emissions: {4:n0}\nTotal transitions: {5:n0}",
            words.Count,
            tags.Count,
            tags.SelectMany(x => x.Emissions).Count(),
            tags.SelectMany(x => x.Transitions).Count(),
            tags.SelectMany(x => x.Emissions).Sum(x => x.Value),
            tags.SelectMany(x => x.Transitions).Sum(x => x.Value));
    }
}
