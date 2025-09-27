// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

// References:
//  - https://en.wikipedia.org/wiki/Viterbi_algorithm#Pseudocode
//  - https://medium.com/data-science-in-your-pocket/pos-tagging-using-hidden-markov-models-hmm-viterbi-algorithm-in-nlp-mathematics-explained-d43ca89347c4
//  - https://en.wiktionary.org/wiki/Appendix:English_suffixes

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NaturalLanguageProcessing.ViterbiTagger;

[Flags]
internal enum Kinds
{
    None = 0,
    Upper = 1,
    Lower = 2,
    Hyphenated = 4,
    Numeral = 8
};

internal static class Program
{
    private const string SentenceStart = "Begin_Sent";
    private const string SentenceEnd = "End_Sent";
    private const int SmoothingK = 1;
    private const int UnknownK = 1;
    private static readonly Dictionary<string, int> words =
        new Dictionary<string, int>();
    private static readonly Dictionary<string, int> tags =
        new Dictionary<string, int>();
    private static readonly Dictionary<(string Word, string Tag), double> likelihood =
        new Dictionary<(string Word, string Tag), double>();
    private static readonly Dictionary<(string Source, string Target), double> transition =
        new Dictionary<(string Source, string Target), double>();
    private static readonly HashSet<string> suffixes = new HashSet<string>()
    {
        "a", "ability", "able", "ably", "ac", "acean", "aceous", "ad", "ade", "aemia", "age", "agog", "agogue", "aholic", "al", "algia", "amine", "an", "ana", "ance", "ancy", "androus", "andry", "ane", "ant", "ar", "arch", "archy", "ard", "arian", "arium", "art", "ary", "ase", "ate", "athon", "ation", "ative", "ator", "atory", "biont", "biosis", "blast", "bot", "cade", "caine", "carp", "carpic", "carpous", "cele", "cene", "centric", "cephalic", "cephalous", "cephaly", "chore", "chory", "chrome", "cide", "clast", "clinal", "cline", "clinic", "coccus", "coel", "coele", "colous", "cracy", "crat", "cratic", "cratical", "cy", "cyte", "dale", "derm", "derma", "dermatous", "dom", "drome", "dromous", "ean", "eaux", "ectomy", "ed", "ee", "eer", "ein", "eme", "emia", "en", "ence", "enchyma", "ency", "ene", "ent", "eous", "er", "ergic", "ergy", "es", "escence", "escent", "ese", "esque", "ess", "est", "et", "eth", "etic", "ette", "ey", "facient", "faction", "fer", "ferous", "fic", "fication", "fid", "florous", "fold", "foliate", "foliolate", "form", "fuge", "ful", "fy", "gamous", "gamy", "gate", "gen", "gene", "genesis", "genetic", "genic", "genous", "geny", "gnathous", "gon", "gony", "gram", "graph", "grapher", "graphy", "gyne", "gynous", "gyny", "hood", "ia", "ial", "ian", "iana", "iasis", "iatric", "iatrics", "iatry", "ibility", "ible", "ic", "icide", "ician", "ics", "id", "ide", "ie", "ify", "ile", "in", "ine", "ing", "ion", "ious", "isation", "ise", "ish", "ism", "ist", "istic", "istical", "istically", "ite", "itious", "itis", "ity", "ium", "ive", "iver", "ix", "ization", "ize", "i", "kin", "kinesis", "kins", "land", "latry", "le", "lepry", "less", "let", "like", "ling", "lite", "lith", "lithic", "log", "logue", "logic", "logical", "logist", "logy", "ly", "lyse", "lysis", "lyte", "lytic", "lyze", "mancy", "mania", "meister", "ment", "mer", "mere", "merous", "meter", "metric", "metrics", "metry", "mire", "mo", "morph", "morphic", "morphism", "morphous", "most", "mycete", "mycin", "nasty", "ness", "nik", "nomy", "nomics", "o", "ode", "odon", "odont", "odontia", "oholic", "oic", "oid", "ol", "ole", "oma", "ome", "omics", "on", "one", "ont", "onym", "onymy", "opia", "opsis", "opsy", "or", "orama", "ory", "ose", "osis", "otic", "otomy", "ous", "o", "para", "parous", "path", "pathy", "ped", "pede", "penia", "petal", "phage", "phagia", "phagous", "phagy", "phane", "phasia", "phil", "phile", "philia", "philiac", "philic", "philous", "phobe", "phobia", "phobic", "phone", "phony", "phore", "phoresis", "phorous", "phrenia", "phyll", "phyllous", "plasia", "plasm", "plast", "plastic", "plasty", "plegia", "plex", "ploid", "pod", "pode", "podous", "poieses", "poietic", "pter", "punk", "rrhagia", "rrhea", "ric", "ry", "'s", "s", "scape", "scope", "scopy", "script", "sect", "sepalous", "ship", "some", "speak", "sperm", "sphere", "sporous", "st", "stasis", "stat", "ster", "stome", "stomy", "taxis", "taxy", "tend", "th", "therm", "thermal", "thermic", "thermy", "thon", "thymia", "tion", "tome", "tomy", "tonia", "trichous", "trix", "tron", "trophic", "trophy", "tropic", "tropism", "tropous", "tropy", "tude", "ture", "ty", "ular", "ule", "ure", "urgy", "uria", "uronic", "urous", "valent", "virile", "vorous", "ward", "wards", "ware", "ways", "wear", "wide", "wise", "worthy", "xor", "y", "yl", "yne", "zilla", "zoic", "zoon", "zygous", "zyme"
    };
    private static int maxSuffixLength;

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(
                "Usage: {0} <pos_file> <words_file>",
                Process.GetCurrentProcess().ProcessName);

            return;
        }

        string posFileName = args[0];
        string wordsFileName = args[1];

        CheckFile(posFileName);
        CheckFile(wordsFileName);

        maxSuffixLength = suffixes.Max(x => x.Length);

        Stopwatch watch = Stopwatch.StartNew();

        ReadPosFile(posFileName);
        TagFile(wordsFileName);
        watch.Stop();
        Console.WriteLine("{0:n0} ms elapsed.", watch.ElapsedMilliseconds);
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
        string previous = SentenceStart;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (previous == SentenceStart)
            {
                tags[SentenceStart] =
                    tags.GetValueOrDefault(SentenceStart) + 1;
            }

            string? word;
            string tag;

            if (string.IsNullOrWhiteSpace(line))
            {
                word = null;
                tag = SentenceEnd;
            }
            else
            {
                string[] segments = line.Split(
                    new string[] { "\t", " " },
                    StringSplitOptions.TrimEntries);

                word = segments[0];

                if (previous == SentenceStart)
                {
                    word = char.ToLower(word[0]) + word.Substring(1);
                }

                tag = segments[1].ToUpperInvariant();
            }

            tags[tag] = tags.GetValueOrDefault(tag) + 1;
            transition[(previous, tag)] =
                transition.GetValueOrDefault((previous, tag)) + 1;

            if (word != null)
            {
                words[word] = words.GetValueOrDefault(word) + 1;
                likelihood[(word, tag)] =
                    likelihood.GetValueOrDefault((word, tag)) + 1;
            }

            if (tag == SentenceEnd)
            {
                previous = SentenceStart;
            }
            else
            {
                previous = tag;
            }
        }

        foreach (string word in words
            .Where(x => x.Value <= UnknownK)
            .Select(x => x.Key)
            .ToList())
        {
            words.Remove(word);

            string guess = TagUnknown(word);

            words[guess] = words.GetValueOrDefault(guess) + 1;

            foreach (string tag in tags.Keys)
            {
                if (likelihood.TryGetValue((word, tag), out double prior))
                {
                    likelihood.Remove((word, tag));

                    likelihood[(guess, tag)] =
                        likelihood.GetValueOrDefault((guess, tag))
                        + prior;
                }
            }
        }

        foreach (KeyValuePair<(string Source, string Target), double> entry in transition)
        {
            transition[entry.Key] =
                (entry.Value + SmoothingK) / (tags[entry.Key.Source] + (SmoothingK * tags.Count));
        }

        foreach (KeyValuePair<(string Word, string Tag), double> entry in likelihood)
        {
            likelihood[entry.Key] =
                (entry.Value + SmoothingK) / (tags[entry.Key.Tag] + (SmoothingK * words.Count));
        }
    }

    private static string TagUnknown(string word)
    {
        Kinds kinds = Kinds.None;

        foreach (char symbol in word)
        {
            if (char.IsUpper(symbol))
            {
                kinds |= Kinds.Upper;
            }

            if (char.IsLower(symbol))
            {
                kinds |= Kinds.Lower;
            }

            if (char.IsDigit(symbol))
            {
                kinds |= Kinds.Numeral;
            }

            switch (symbol)
            {
                case '-':
                    kinds |= Kinds.Hyphenated;
                    break;
            }
        }

        return string.Format(
            "Unknown_Word[{0},{1}]",
            (int)kinds,
            GetSuffix(word) ?? string.Empty);
    }

    private static string? GetSuffix(string word)
    {
        for (int i = Math.Min(maxSuffixLength, word.Length); i >= 1; i--)
        {
            string suffix = word.Substring(word.Length - i, i);

            if (suffixes.Contains(suffix))
            {
                return suffix;
            }
        }

        return null;
    }

    private static void TagFile(string fileName)
    {
        using StreamReader reader = File.OpenText(fileName);
        using StreamWriter writer = File.CreateText("submission.pos");

        List<string> sentence = new List<string>();
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                RealizeSentence(writer, sentence);
            }
            else
            {
                sentence.Add(line);
            }
        }

        RealizeSentence(writer, sentence);
    }

    private static void RealizeSentence(
        StreamWriter writer,
        List<string> sentence)
    {
        if (sentence.Count == 0)
        {
            return;
        }

        string[] tags = TagSentence(sentence);

        for (int i = 0; i < sentence.Count; i++)
        {
            writer.WriteLine("{0}\t{1}", sentence[i], tags[i]);
        }

        writer.WriteLine();
        sentence.Clear();
    }

    private static string[] TagSentence(List<string> sentence)
    {
        string[] states = new string[tags.Count];

        states[0] = SentenceStart;

        int j = 1;

        foreach (string tag in tags.Keys)
        {
            if (tag != SentenceStart && tag != SentenceEnd)
            {
                states[j] = tag;
                j++;
            }
        }

        states[states.Length - 1] = SentenceEnd;

        double[,] viterbi = new double[sentence.Count + 1, tags.Count];

        viterbi[0, 0] = 1;

        int[,] backpointer = new int[sentence.Count + 1, tags.Count];
        int argMax;
        double max;

        for (int t = 1; t <= sentence.Count; t++)
        {
            string word = sentence[t - 1];

            for (int q = 0; q < states.Length; q++)
            {
                argMax = -1;
                max = -1;

                for (int p = 0; p < states.Length; p++)
                {
                    double current =
                        viterbi[t - 1, p]
                        * transition.GetValueOrDefault((states[p], states[q]))
                        * GetLikelihood(word, states[q]);

                    if (current > max)
                    {
                        argMax = p;
                        max = current;
                    }
                }

                if (argMax != -1)
                {
                    viterbi[t, q] = max;
                    backpointer[t, q] = argMax;
                }
            }
        }

        argMax = -1;
        max = -1;

        for (int p = 0; p < states.Length; p++)
        {
            double current =
                transition.GetValueOrDefault((states[p], SentenceEnd))
                * viterbi[sentence.Count, p];

            if (current > max)
            {
                argMax = p;
                max = current;
            }
        }

        if (argMax == -1)
        {
            return Array.Empty<string>();
        }

        string[] results = new string[sentence.Count];

        results[results.Length - 1] = states[argMax];

        for (int i = backpointer.GetLength(0) - 1; i > 1; i--)
        {
            argMax = backpointer[i, argMax];
            results[i - 2] = states[argMax];
        }

        return results;
    }

    private static double GetLikelihood(string word, string tag)
    {
        if (words.ContainsKey(word))
        {
            return likelihood.GetValueOrDefault((word, tag));
        }

        string guess = TagUnknown(word);

        if (words.ContainsKey(guess))
        {
            return likelihood.GetValueOrDefault((guess, tag));
        }

        return 1d / 1000;
    }
}
