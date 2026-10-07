using System.Text;

namespace CgCar.Core.Models;

/// <summary>
/// Converts plate numbers to one canonical form so that "ΙΚΑ-1234", "ika 1234" and "IKA1234" count as the same plate.
/// Greek plates only use the 14 capital letters that look identical in both alphabets, so users may type either one.
/// </summary>
public static class LicensePlate
{
    private static readonly Dictionary<char, char> GreekToLatin = new()
    {
        ['Α'] = 'A', ['Β'] = 'B', ['Ε'] = 'E', ['Ζ'] = 'Z', ['Η'] = 'H', ['Ι'] = 'I', ['Κ'] = 'K',
        ['Μ'] = 'M', ['Ν'] = 'N', ['Ο'] = 'O', ['Ρ'] = 'P', ['Τ'] = 'T', ['Υ'] = 'Y', ['Χ'] = 'X',
    };

    /// <summary>Uppercases, removes accents, spaces and separators, and maps Greek look-alike letters to Latin.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // FormD splits accented letters ("ά") into the base letter plus a separate accent mark,
        // which the IsLetterOrDigit check below then drops along with spaces and dashes.
        var decomposed = input.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (!char.IsLetterOrDigit(ch))
            {
                continue;
            }

            var upper = char.ToUpperInvariant(ch);
            result.Append(GreekToLatin.TryGetValue(upper, out var latin) ? latin : upper);
        }

        return result.ToString();
    }

    /// <summary>Display form of a normalized plate: "IKA1234" becomes "IKA-1234". Other shapes are returned unchanged.</summary>
    public static string Format(string? normalized)
    {
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        var lettersEnd = 0;
        while (lettersEnd < normalized.Length && char.IsLetter(normalized[lettersEnd]))
        {
            lettersEnd++;
        }

        var hasLetters = lettersEnd > 0;
        var restIsDigits = lettersEnd < normalized.Length && normalized[lettersEnd..].All(char.IsAsciiDigit);

        return hasLetters && restIsDigits
            ? $"{normalized[..lettersEnd]}-{normalized[lettersEnd..]}"
            : normalized;
    }
}
