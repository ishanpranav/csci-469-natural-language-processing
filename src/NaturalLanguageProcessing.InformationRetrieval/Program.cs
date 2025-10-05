// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NaturalLanguageProcessing.InformationRetrieval;

internal sealed class SparseVector
{
    private readonly Dictionary<string, double> _entries =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

    public double this[string token]
    {
        get
        {
            return _entries.GetValueOrDefault(token);
        }
        set
        {
            _entries[token] = value;
        }
    }

    public static double CosineSimilarity(SparseVector left, SparseVector right)
    {
        double dot = 0;
        double magnitudeA = 0;

        foreach (KeyValuePair<string, double> pair in left._entries)
        {
            double b = pair.Value;

            dot += b * right[pair.Key];
            magnitudeA += b * b;
        }

        if (magnitudeA == 0)
        {
            return 0;
        }

        double magnitudeB = 0;

        foreach (double b in right._entries.Values)
        {
            magnitudeB += b * b;
        }

        if (magnitudeB == 0)
        {
            return 0;
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        return dot / (magnitudeA * magnitudeB);
    }
}

internal sealed partial class Article
{
    private static readonly string[] delimiters = { " ", "\t", "\r", "\n" };
    private static readonly HashSet<string> stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a", "the", "an", "and", "or", "but", "about", "above", "after",
        "along", "amid", "among", "as", "at", "by", "for", "from", "in",
        "into", "like", "minus", "near", "of", "off", "on",
        "onto", "out", "over", "past", "per", "plus", "since", "till", "to",
        "under", "until", "up", "via", "vs", "with", "that", "can", "cannot",
        "could", "may", "might", "must", "need", "ought", "shall", "should",
        "will", "would", "have", "had", "has", "having", "be", "is", "am",
        "are", "was", "were", "being", "been", "get", "gets", "got", "gotten",
        "getting", "seem", "seeming", "seems", "seemed", "enough", "both",
        "all", "those", "this", "these", "their", "the", "that", "some", "our",
        "no", "neither", "my", "its", "his", "her", "every", "either", "each",
        "any", "another", "an", "a", "just", "mere", "such", "merely", "right",
        "no", "not", "only", "sheer", "even", "especially", "namely", "as",
        "more",  "most", "less", "least", "so", "enough", "too", "pretty",
        "quite", "rather", "somewhat", "sufficiently", "same", "different",
        "such", "when", "why", "where", "how", "what", "who", "whom", "which",
        "whether", "why", "whose", "if", "anybody", "anyone", "anyplace",
        "anything", "anytime", "anywhere", "everybody", "everyday",
        "everyone", "everyplace", "everything", "everywhere", "whatever",
        "whenever", "wherever", "whichever", "whoever", "whomever", "he",
        "him", "his", "her", "she", "it", "they", "them", "its", "their",
        "theirs", "you", "your", "yours", "me", "my", "mine", "I", "we", "us",
        "much", "and/or"
    };

    [GeneratedRegex(
        @"[^A-Za-z\s]",
        RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();

    public Article(int id, string title, string summary)
    {
        Id = id;
        Title = title;
        Tokens = TokenRegex()
            .Replace(summary, " ")
            .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !stopWords.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public int Id { get; }
    public string Title { get; }
    public HashSet<string> Tokens { get; } = new HashSet<string>();
    
    public SparseVector Vectorize(HashSet<string> tokens, SparseVector idf)
    {
        SparseVector result = new SparseVector();
        Dictionary<string, int> tf =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in Tokens)
        {
            tf[token] = tf.GetValueOrDefault(token) + 1;
        }

        foreach (string token in tf.Keys)
        {
            if (!tokens.Contains(token))
            {
                continue;
            }

            result[token] = tf[token] * idf[token];
        }

        return result;
    }
}

internal static class Program
{
    private static readonly HashSet<string> tokens = new HashSet<string>();
    private static readonly Dictionary<string, int> df =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly SparseVector idf = new SparseVector();

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(
                "Usage: {0} <file> <qry_file>",
                Process.GetCurrentProcess().ProcessName);

            return;
        }

        string fileName = args[0];
        string qryFileName = args[1];

        CheckFile(fileName);
        CheckFile(qryFileName);

        List<Article> articles = ReadFile(fileName);

        foreach (Article article in articles)
        {
            tokens.UnionWith(article.Tokens);

            foreach (string token in article.Tokens)
            {
                df[token] = df.GetValueOrDefault(token) + 1;
            }
        }

        double nu = articles.Count + 1;

        foreach (string token in tokens)
        {
            idf[token] = Math.Log(nu / (1d  + df.GetValueOrDefault(token))) + 1;
        }

        List<Article> queries = ReadFile(qryFileName);
    }

    private static void CheckFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            Console.WriteLine("File does not exist: \"{0}\".", fileName);
            Environment.Exit(0);
        }
    }

    private static List<Article> ReadFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);

        string? line;
        char section = '\0';
        int id = 0;
        StringBuilder titleBuilder = new StringBuilder();
        StringBuilder authorBuilder = new StringBuilder();
        StringBuilder metadataBuilder = new StringBuilder();
        StringBuilder summaryBuilder = new StringBuilder();
        List<Article> articles = new List<Article>();

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line[0] == '.')
            {
                if (line.Length < 2)
                {
                    throw new FormatException();
                }

                section = line[1];
                line = line.Substring(2);
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            switch (section)
            {
                case 'I':
                    RealizeArticle(
                        ref id,
                        titleBuilder,
                        summaryBuilder,
                        articles);

                    id = int.Parse(line);
                    break;

                case 'T':
                    titleBuilder.AppendLine(line);
                    break;

                case 'W':
                    summaryBuilder.AppendLine(line);
                    break;
            }
        }

        RealizeArticle(
            ref id,
            titleBuilder,
            summaryBuilder,
            articles);

        return articles;
    }

    private static void RealizeArticle(
        ref int id,
        StringBuilder titleBuilder,
        StringBuilder summaryBuilder,
        List<Article> articles)
    {
        if (id == 0)
        {
            return;
        }

        articles.Add(new Article(
            id,
            titleBuilder.ToString(),
            summaryBuilder.ToString()));

        id = 0;
        titleBuilder.Clear();
        summaryBuilder.Clear();
    }
}
