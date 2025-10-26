// Program.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace NaturalLanguageProcessing.NounGroupTagger;

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


    }
}
