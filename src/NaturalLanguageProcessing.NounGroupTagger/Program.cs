// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using Porter2Stemmer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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
    private const string SentenceStart = "*B*";
    private const string SentenceEnd = "*E*";
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

    private static IEnumerable<List<string>> GenerateFeatures(IReadOnlyList<Token> tokens)
    {
        List<Token> sentence = new List<Token>()
        {
            new Token(SentenceStart + "-2", SentenceStart + "-2", "O"),
            new Token(SentenceStart + "-1", SentenceStart + "-1", "O")
        };

        sentence.AddRange(tokens);
        sentence.Add(new Token(SentenceEnd + "+1", SentenceEnd + "+1", "O"));
        sentence.Add(new Token(SentenceEnd + "+2", SentenceEnd + "+2", "O"));

        for (int i = 2; i < sentence.Count - 2; i++)
        {
            Token token = sentence[i];
            List<string> features = new List<string>()
            {
                token.Word,
                $"word={token.Word}",
                $"pos={token.Pos}",
                $"word_pos={token.Word}_{token.Pos}",
                $"length={token.Word.Length}",
                $"stem={stemmer.Stem(token.Word).Value}",
                $"all_upper={token.Word.All(char.IsUpper)}",
                $"first_upper={char.IsUpper(token.Word[0])}",
                "wildcard_bio=@@",
                $"previous_previous_word={sentence[i - 2].Word}",
                $"previous_previous_pos-{sentence[i - 2].Pos}",
                $"next_word={sentence[i + 1].Word}",
                $"next_pos={sentence[i + 1].Pos}",
                $"next_word_pos={sentence[i + 1].Word}_{token.Pos}",
                $"next_next_word={sentence[i + 2].Word}",
                $"next_next_pos={sentence[i + 2].Pos}",
                $"bigram_left={sentence[i - 1].Pos}_{token.Pos}",
                $"bigram_left={token.Pos}_{sentence[i + 1].Pos}",
                $"trigram={sentence[i - 1].Pos}_{token.Pos}_{sentence[i + 1].Pos}",
                $"fourgram_left={sentence[i - 2].Pos}_{sentence[i - 1].Pos}_{token.Pos}_{sentence[i + 1].Pos}",
                $"fourgram_right={sentence[i - 1].Pos}_{token.Pos}_{sentence[i + 1].Pos}_{sentence[i + 2].Pos}",
                $"fivegram={sentence[i - 2].Pos}_{sentence[i - 1].Pos}_{token.Pos}_{sentence[i + 1].Pos}_{sentence[i + 2].Pos}",
                $"previous_word={sentence[i - 1].Word}",
                $"previous_pos-{sentence[i - 1].Pos}",
                $"previous_bio={sentence[i - 1].Bio ?? "O"}",
                $"previous_word_pos={sentence[i - 1].Word}_{token.Pos}",
            };

            if (token.Pos.StartsWith("VB"))
            {
                features.Add("verb");
            }

            if (token.Pos.StartsWith("NN"))
            {
                features.Add("noun");
            }

            if (token.Pos.StartsWith("JJ"))
            {
                features.Add("adjective");
            }

            if (sentence[i].Pos == "IN" || sentence[i].Pos == "TO")
            {
                features.Add("preposition");
            }

            if (sentence[i].Pos.EndsWith("DT"))
            {
                features.Add("determiner");
            }

            if (sentence[i - 1].Pos.StartsWith("VB"))
            {
                features.Add("previous_verb");
            }

            if (sentence[i - 1].Pos.StartsWith("NN"))
            {
                features.Add("previous_noun");
            }

            if (sentence[i - 1].Pos.StartsWith("JJ"))
            {
                features.Add("previous_adjective");
            }

            if (sentence[i - 1].Pos == "IN" || sentence[i - 1].Pos == "TO")
            {
                features.Add("previous_preposition");
            }

            if (sentence[i - 1].Pos.EndsWith("DT"))
            {
                features.Add("previous_determiner");
            }

            string shape = GetShape(token.Word);

            features.Add($"shape={shape}");
            features.Add($"compressed_shape={Compress(shape)}");

            for (int k = 2; k <= 4; k++)
            {
                if (token.Word.Length >= k)
                {
                    features.Add($"first{k}={token.Word.Substring(0, k)}");
                    features.Add($"last{k}={token.Word.Substring(token.Word.Length - k, k)}");
                }
                else
                {
                    features.Add($"first{k}={token.Word}");
                    features.Add($"last{k}={token.Word}");
                }
            }

            Features f = GetFeatures(token.Word);

            for (int flag = 1; flag <= (1 << 31); flag <<= 1)
            {
                if ((f & (Features)flag) != 0)
                {
                    features.Add(((Features)flag).ToString());
                }
            }

            if (token.Bio != null)
            {
                features.Add(token.Bio);
            }

            yield return features;
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

    private static string GetShape(string word)
    {
        StringBuilder result = new StringBuilder();

        foreach (char symbol in word)
        {
            if (char.IsDigit(symbol))
            {
                result.Append('#');
            }
            else if (char.IsUpper(symbol))
            {
                result.Append('A');
            }
            else if (char.IsLower(symbol))
            {
                result.Append('a');
            }
        }

        return result.ToString();
    }

    private static string Compress(string shape)
    {
        StringBuilder result = new StringBuilder();

        foreach (char symbol in shape)
        {
            if (result.Length == 0 || result[result.Length - 1] != symbol)
            {
                result.Append(symbol);
            }
        }

        return result.ToString();
    }
}
