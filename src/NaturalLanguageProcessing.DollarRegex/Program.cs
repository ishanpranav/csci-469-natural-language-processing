using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace NaturalLanguageProcessing.DollarRegex;

internal static partial class Program
{
    [GeneratedRegex(
        @"(?:\$(?:-?\d{1,3}(?:,\d{3})*|\d+)(?:\.\d+)?\s?(?:MILLION|BILLION|TRILLION)?(?:\s?(?:DOLLARS?|CENTS?))?(?:\s?AND\s?\d+\s?CENTS?)?)|(?:(?:(?:(?:(?:HALF|A|ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE|TEN|ELEVEN|TWELVE|THIRTEEN|FOURTEEN|FIFTEEN|SIXTEEN|SEVENTEEN|EIGHTEEN|NINETEEN|TWENTY|THIRTY|FOURTY|FIFTY|SIXTY|SEVENTY|EIGHTY|NINETY|HUNDRED|THOUSAND|MILLION|BILLION|TRILLION|AND)\s?)+)|(?:(-?\d{1,3}(?:,\d{3})*|\d+)(?:\.\d+)?\s?))(?:MILLION|BILLION|TRILLION)?(\s?(?:DOLLARS?|CENTS?))(\s?AND\s?\d+\s?CENTS?)?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex DollarRegex();

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
        Regex regex = DollarRegex();

        while ((line = reader.ReadLine()) != null)
        {
            foreach (Match match in regex.Matches(line))
            {
                Console.WriteLine(match.Value.Trim());
            }
        }
    }
}
