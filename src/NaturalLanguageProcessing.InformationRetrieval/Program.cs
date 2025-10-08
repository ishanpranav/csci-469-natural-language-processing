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

internal sealed class Document
{
    private int _n;
    private IReadOnlyDictionary<string, double>? _idf;
    private Dictionary<string, double>? _tfidf = null;

    public Document(IReadOnlyCollection<string> tokens)
    {
        Tokens = tokens;

        Dictionary<string, int> tf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in tokens)
        {
            tf[token] = tf.GetValueOrDefault(token) + 1;
        }

        TermFrequencies = tf;
    }

    public IReadOnlyCollection<string> Tokens { get; }
    public IReadOnlyDictionary<string, int> TermFrequencies { get; }

    public IReadOnlyDictionary<string, double> GetOrComputeTfidf(int n, IReadOnlyDictionary<string, double> idf)
    {
        if (_tfidf != null && _n == n && _idf == idf)
        {
            return _tfidf;
        }

        Dictionary<string, double> results =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, int> entry in TermFrequencies)
        {
            double tfValue = entry.Value > 0 ? 1.0 + Math.Log(entry.Value) : 0.0;
            double idfValue = idf.ContainsKey(entry.Key) ? idf[entry.Key] : Math.Log(n / 1d);

            results[entry.Key] = tfValue * idfValue;
        }

        _tfidf = results;
        _n = n;
        _idf = idf;

        return _tfidf;
    }
}

internal static partial class Program
{
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

    [GeneratedRegex(@"[A-Za-z]+", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: {0} <file> <qry_file>", Process.GetCurrentProcess().ProcessName);

            return;
        }

        string fileName = args[0];
        string qryFileName = args[1];

        CheckFile(fileName);
        CheckFile(qryFileName);

        List<Document> articles = ReadFile(fileName);
        List<Document> queries = ReadFile(qryFileName);
        Dictionary<string, double> articleIdf =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, double> queryIdf =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        GetIdf(articleIdf, articles);
        GetIdf(queryIdf, queries);

        using (var writer = new StreamWriter("output.txt"))
        {
            for (int i = 0; i < queries.Count; i++)
            {
                IReadOnlyDictionary<string, double> queryVector =
                    queries[i].GetOrComputeTfidf(queries.Count, queryIdf);
                double normalizedQuery = Math.Sqrt(queryVector.Sum(x => x.Value * x.Value));
                List<(int articleId, double score)> results = new List<(int articleId, double score)>();

                for (int j = 0; j < articles.Count; ++j)
                {
                    IReadOnlyDictionary<string, double> articleVector =
                        articles[j].GetOrComputeTfidf(articles.Count, articleIdf);

                    results.Add((j + 1, CosSimilarity(normalizedQuery, articleVector, queryVector)));
                }

                foreach ((int articleId, double score) in results
                    .OrderByDescending(x => x.score)
                    .ThenBy(x => x.articleId))
                {
                    writer.WriteLine("{0} {1} {2}", i + 1, articleId, score);
                }
            }
        }

        Console.WriteLine("Done.");
    }

    private static void CheckFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            Console.WriteLine("File does not exist: \"{0}\".", fileName);
            Environment.Exit(0);
        }
    }

    private static List<Document> ReadFile(string filename)
    {
        using StreamReader reader = File.OpenText(filename);

        string? line;
        char section = '\0';
        int id = 0;
        StringBuilder abstractBuilder = new StringBuilder();
        List<Document> results = new List<Document>();

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
                    RealizeDocument(ref id, abstractBuilder, results);

                    id = int.Parse(line);
                    break;

                case 'W':
                    abstractBuilder.AppendLine(line);
                    break;
            }
        }

        RealizeDocument(ref id, abstractBuilder, results);

        return results;
    }

    private static void RealizeDocument(
        ref int id,
        StringBuilder summaryBuilder,
        List<Document> documents)
    {
        if (id == 0)
        {
            return;
        }

        id = 0;

        List<string> tokens = TokenRegex()
            .Matches(summaryBuilder.ToString())
            .Select(x => x.Value.ToLowerInvariant())
            .Where(x => x.Length > 0 && !stopWords.Contains(x))
            .ToList();

        documents.Add(new Document(tokens));
        summaryBuilder.Clear();
    }

    private static void GetIdf(Dictionary<string, double> results, IReadOnlyCollection<Document> documents)
    {
        foreach (Document document in documents)
        {
            foreach (string token in document.TermFrequencies.Keys)
            {
                results[token] = results.GetValueOrDefault(token) + 1;
            }
        }

        foreach (KeyValuePair<string, double> entry in results)
        {
            results[entry.Key] = Math.Log(documents.Count / (entry.Value + 1));
        }
    }

    private static double CosSimilarity(
        double normalizedQuery,
        IReadOnlyDictionary<string, double> articleVector,
        IReadOnlyDictionary<string, double> queryVector)
    {
        if (normalizedQuery == 0)
        {
            return 0;
        }

        double normalizedArticle = Math.Sqrt(articleVector.Sum(x => x.Value * x.Value));

        if (normalizedArticle == 0)
        {
            return 0;
        }

        double dot = 0.0;

        foreach (string token in queryVector.Keys)
        {
            dot += queryVector[token] * articleVector.GetValueOrDefault(token);
        }

        return dot / (normalizedQuery * normalizedArticle);
    }
}
