// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

// References:
//  - https://gist.github.com/sebleier/554280 (NLTK's list of english stopwords)
// This resource gives a list of English stopwords. Including these words
// improves the MAP score.

using Porter2Stemmer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NaturalLanguageProcessing.InformationRetrieval;

internal sealed class Document
{
    private const int TitleWeight = 4;

    private IReadOnlyDictionary<string, double>? _idf;
    private Dictionary<string, double>? _tfidf = null;

    public Document(IReadOnlyCollection<string> titleTokens, IReadOnlyCollection<string> summaryTokens)
    {
        TitleTokens = titleTokens;
        SummaryTokens = summaryTokens;

        Dictionary<string, int> tf =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in summaryTokens)
        {
            tf[token] = tf.GetValueOrDefault(token) + 1;
        }

        foreach (string token in titleTokens)
        {
            tf[token] = tf.GetValueOrDefault(token) + TitleWeight;
        }

        TermFrequencies = tf;
    }

    public IReadOnlyCollection<string> TitleTokens { get; }
    public IReadOnlyCollection<string> SummaryTokens { get; }
    public IReadOnlyDictionary<string, int> TermFrequencies { get; }

    public IReadOnlyDictionary<string, double> GetOrComputeTfidf(IReadOnlyDictionary<string, double> idf)
    {
        if (_tfidf != null && _idf == idf)
        {
            return _tfidf;
        }

        Dictionary<string, double> results =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        int count = SummaryTokens.Count + TitleTokens.Count * TitleWeight;

        foreach (KeyValuePair<string, int> entry in TermFrequencies)
        {
            if (entry.Value == 0 || !idf.ContainsKey(entry.Key))
            {
                continue;
            }

            double tfValue = (Math.Log(entry.Value) + 1d) / count;
            double idfValue = idf[entry.Key];

            results[entry.Key] = tfValue * idfValue;
        }

        _tfidf = results;
        _idf = idf;

        return _tfidf;
    }
}

internal static partial class Program
{
    private static readonly HashSet<string> stopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Professor's stop-list
            
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
            "much", "and/or",

            // NLTK's English stop-list

            "a", "about", "above", "after", "again", "against", "ain", "all", "am", "an", "and", "any", "are", "aren", "aren't", "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by", "can", "couldn", "couldn't", "d", "did", "didn", "didn't", "do", "does", "doesn", "doesn't", "doing", "don", "don't", "down", "during", "each", "few", "for", "from", "further", "had", "hadn", "hadn't", "has", "hasn", "hasn't", "have", "haven", "haven't", "having", "he", "he'd", "he'll", "her", "here", "hers", "herself", "he's", "him", "himself", "his", "how", "i", "i'd", "if", "i'll", "i'm", "in", "into", "is", "isn", "isn't", "it", "it'd", "it'll", "it's", "its", "itself", "i've", "just", "ll", "m", "ma", "me", "mightn", "mightn't", "more", "most", "mustn", "mustn't", "my", "myself", "needn", "needn't", "no", "nor", "not", "now", "o", "of", "off", "on", "once", "only", "or", "other", "our", "ours", "ourselves", "out", "over", "own", "re", "s", "same", "shan", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn", "shouldn't", "should've", "so", "some", "such", "t", "than", "that", "that'll", "the", "their", "theirs", "them", "themselves", "then", "there", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through", "to", "too", "under", "until", "up", "ve", "very", "was", "wasn", "wasn't", "we", "we'd", "we'll", "we're", "were", "weren", "weren't", "we've", "what", "when", "where", "which", "while", "who", "whom", "why", "will", "with", "won", "won't", "wouldn", "wouldn't", "y", "you", "you'd", "you'll", "your", "you're", "yours", "yourself", "yourselves", "you've"
        };
    private static readonly EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();

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
        Dictionary<string, double> articleIdf = GetIdf(articles);
        Dictionary<string, double> queryIdf = GetIdf(queries);

        using StreamWriter writer = File.CreateText("output.txt");

        for (int i = 0; i < queries.Count; i++)
        {
            IReadOnlyDictionary<string, double> queryVector =
                queries[i].GetOrComputeTfidf(queryIdf);
            double normalizedQuery = Math.Sqrt(queryVector.Sum(x => x.Value * x.Value));
            List<(int articleId, double score)> results = new List<(int articleId, double score)>();

            for (int j = 0; j < articles.Count; ++j)
            {
                IReadOnlyDictionary<string, double> articleVector =
                    articles[j].GetOrComputeTfidf(articleIdf);

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
        StringBuilder titleBuilder = new StringBuilder();
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
                    RealizeDocument(ref id, titleBuilder, abstractBuilder, results);

                    id = int.Parse(line);
                    break;

                case 'T':
                    titleBuilder.AppendLine(line);
                    break;

                case 'W':
                    abstractBuilder.AppendLine(line);
                    break;
            }
        }

        RealizeDocument(ref id, titleBuilder, abstractBuilder, results);

        return results;
    }

    private static void RealizeDocument(
        ref int id,
        StringBuilder titleBuilder,
        StringBuilder summaryBuilder,
        List<Document> documents)
    {
        if (id == 0)
        {
            return;
        }

        id = 0;

        List<string> titleTokens = Tokenize(titleBuilder.ToString());
        List<string> summaryTokens = Tokenize(summaryBuilder.ToString());

        documents.Add(new Document(titleTokens, summaryTokens));
        titleBuilder.Clear();
        summaryBuilder.Clear();
    }

    private static List<string> Tokenize(string value)
    {
        return TokenRegex()
            .Matches(value)
            .Select(x => x.Value)
            .Where(x => x.Length > 0 && !stopWords.Contains(x))
            .Select(x => stemmer.Stem(x).Value)
            .ToList();
    }

    private static Dictionary<string, double> GetIdf(IReadOnlyCollection<Document> documents)
    {
        Dictionary<string, double> results =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (Document document in documents)
        {
            foreach (string token in document.TermFrequencies.Keys)
            {
                results[token] = results.GetValueOrDefault(token) + 1;
            }
        }

        foreach (KeyValuePair<string, double> entry in results)
        {
            results[entry.Key] = Math.Log((documents.Count + 1) / (entry.Value + 1)) + 1;
        }

        return results;
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

        double dot = queryVector.Keys.Sum(x => queryVector[x] * articleVector.GetValueOrDefault(x));

        return dot / (normalizedQuery * normalizedArticle);
    }
}
