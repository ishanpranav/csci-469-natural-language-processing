// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace NaturalLanguageProcessing.TelephoneRegex;

internal static partial class Program
{
    [GeneratedRegex(
        @"(?:\+\d{1,2}\s*)?\(?\d{3}\)?(?:[\s\-\.])*\d{3}(?:[\s\-\.])*\d{4}",
        RegexOptions.IgnoreCase)]
    private static partial Regex TelephoneRegex();

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
        Regex regex = TelephoneRegex();

        while ((line = reader.ReadLine()) != null)
        {
            foreach (Match match in regex.Matches(line))
            {
                Console.WriteLine(match.Value.Trim());
            }
        }
    }
}
