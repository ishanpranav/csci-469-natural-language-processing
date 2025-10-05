// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NaturalLanguageProcessing.InformationRetrieval;

internal class Article
{
    public int Id { get; }
    public string Title { get; }
    public string Author { get; }
    public string Metadata { get; }
    public string Summary { get; }

    public Article(int id, string title, string author, string metadata, string summary)
    {
        Id = id;
        Title = title;
        Author = author;
        Metadata = metadata;
        Summary = summary;
    }
}

internal static class Program
{
    private static readonly HashSet<string> stopWords = new HashSet<string>()
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
                        authorBuilder,
                        metadataBuilder,
                        summaryBuilder,
                        articles);

                    id = int.Parse(line);
                    break;

                case 'T':
                    titleBuilder.AppendLine(line);
                    break;

                case 'A':
                    authorBuilder.AppendLine(line);
                    break;

                case 'B':
                    metadataBuilder.AppendLine(line);
                    break;

                case 'W':
                    summaryBuilder.AppendLine(line);
                    break;
            }
        }

        RealizeArticle(
            ref id,
            titleBuilder,
            authorBuilder,
            metadataBuilder,
            summaryBuilder,
            articles);

        return articles;
    }

    private static void RealizeArticle(
        ref int id,
        StringBuilder titleBuilder,
        StringBuilder authorBuilder,
        StringBuilder metadataBuilder,
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
            authorBuilder.ToString(),
            metadataBuilder.ToString(),
            summaryBuilder.ToString()));

        id = 0;
        titleBuilder.Clear();
        authorBuilder.Clear();
        metadataBuilder.Clear();
        summaryBuilder.Clear();
    }
}
