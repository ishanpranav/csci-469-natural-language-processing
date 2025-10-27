// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using Porter2Stemmer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace NaturalLanguageProcessing.NounGroupTagger;

[Flags]
internal enum Features
{
    None = 0,
    Upper = 1,
    Lower = 2,
    Hyphenated = 4,
    Numeral = 8
}

internal sealed class Token
{
    public Token(string word, string pos, string? bio)
    {
        Word = word;
        Pos = pos;
        Bio = bio;
    }
    public string Word { get; }
    public string Pos { get; }
    public string? Bio { get; }
}

internal static class Program
{
    private static readonly string[] delimiters = { "\t", " " };
    private static readonly List<IReadOnlyList<Token>> sentences =
        new List<IReadOnlyList<Token>>();
    private static readonly EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: {0} <input_file> <output_file>",
                Process.GetCurrentProcess().ProcessName);

            return;
        }

        string inputFileName = args[0];

        if (!File.Exists(inputFileName))
        {
            Console.WriteLine("File does not exist: \"{0}\".", inputFileName);

            return;
        }

        ReadFile(inputFileName);

        using StreamWriter writer = File.CreateText(args[1]);

        foreach (IReadOnlyList<Token> sentence in sentences)
        {
            if (sentence.Count == 0)
            {
                writer.WriteLine();

                continue;
            }

            foreach (List<string> feature in GenerateFeatures(sentence))
            {
                writer.WriteLine(string.Join("\t", feature));
            }
        }
    }

    private static void ReadFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);

        string? line;
        List<Token> sentence = new List<Token>();

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                RealizeSentence(ref sentence);

                sentence = new List<Token>();
                sentences.Add(Array.Empty<Token>());

                continue;
            }

            string[] segments = line.Split(delimiters,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
            {
                throw new FormatException();
            }

            sentence.Add(new Token(
                word: segments[0],
                pos: segments[1],
                bio: segments.Length > 2 ? segments[2] : null));
        }

        RealizeSentence(ref sentence);
    }

    private static void RealizeSentence(ref List<Token> sentence)
    {
        if (sentence.Count == 0)
        {
            return;
        }

        sentences.Add(sentence);

        sentence = new List<Token>();
    }

    private static IEnumerable<List<string>> GenerateFeatures(IReadOnlyList<Token> sentence)
    {
        List<string>[] baseFeatures = new List<string>[sentence.Count];
        List<string>[] features = new List<string>[sentence.Count];

        for (int i = 0; i < sentence.Count; i++)
        {
            baseFeatures[i] = new List<string>();
            features[i] = new List<string>() { sentence[i].Word };

            AddFeatures(baseFeatures[i], sentence[i]);
            features[i].AddRange(baseFeatures[i]);
        }

        for (int i = 1; i < sentence.Count - 1; i++)
        {
            AddFeaturesWithPrefix(features[i - 1], baseFeatures[i], "previous__");
        }

        for (int i = 0; i < sentence.Count; i++)
        {
            if (sentence[i].Bio != null)
            {
                features[i].Add(sentence[i].Bio!);
            }
        }

        return features;
    }

    private static void AddFeaturesWithPrefix(
        List<string> results, 
        List<string> features, 
        string prefix)
    {
        foreach (string feature in features)
        {
            results.Add($"{prefix}__{feature}");
        }
    }

    private static void AddFeatures(List<string> results, Token token)
    {
        results.Add($"word={token.Word}");
        results.Add($"pos={token.Pos}");
        results.Add($"first_upper={char.IsUpper(token.Word[0])}");
        results.Add($"length={token.Word.Length}");
        results.Add($"stem={stemmer.Stem(token.Word).Value}");

        Features features = GetFeatures(token.Word);

        for (int flag = 1; flag <= (1 << 31); flag <<= 1)
        {
            if ((features & (Features)flag) != 0)
            {
                results.Add(((Features)flag).ToString());
            }
        }

        if (token.Bio != null)
        {
            results.Add($"bio={token.Bio}");
        }
    }

    private static Features GetFeatures(string word)
    {
        Features result = Features.None;

        for (int i = 0; i < word.Length; i++)
        {
            char symbol = word[i];

            if (char.IsUpper(symbol))
            {
                result |= Features.Upper;
            }

            if (i > 0 && char.IsLower(symbol))
            {
                result |= Features.Lower;
            }

            if (char.IsDigit(symbol))
            {
                result |= Features.Numeral;
            }

            switch (symbol)
            {
                case '-':
                    result |= Features.Hyphenated;
                    break;
            }
        }

        return result;
    }
}
