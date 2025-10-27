// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NaturalLanguageProcessing.NounGroupTagger;

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
        for (int i = 0; i < sentence.Count; i++)
        {
            Token token = sentence[i];
            List<string> features = new List<string>()
            {
                token.Word,
                $"pos={token.Pos}"
            };

            if (token.Bio != null)
            {
                features.Add(token.Bio);
            }

            yield return features;
        }
    }
}
