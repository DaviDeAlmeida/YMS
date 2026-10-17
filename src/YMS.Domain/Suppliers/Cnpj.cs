namespace YMS.Domain.Suppliers;

/// <summary>Brazilian company registry number (14 digits, the last two being check digits).</summary>
public static class Cnpj
{
    /// <summary>Digits of a CNPJ without its mask.</summary>
    public const int Length = 14;

    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Drops the usual mask characters (<c>.</c>, <c>/</c>, <c>-</c> and spaces), keeping everything else.</summary>
    public static string Normalize(string value) =>
        new(value.Where(c => c is not ('.' or '/' or '-' or ' ')).ToArray());

    /// <summary>True for 14 digits, not all equal, with valid check digits. Accepts the masked or unmasked form.</summary>
    public static bool IsValid(string? value)
    {
        if (value is null)
            return false;

        var digits = Normalize(value);
        if (digits.Length != Length || !digits.All(char.IsAsciiDigit) || digits.Distinct().Count() == 1)
            return false;

        return CheckDigit(digits, FirstWeights) == digits[12] - '0'
            && CheckDigit(digits, SecondWeights) == digits[13] - '0';
    }

    private static int CheckDigit(string digits, int[] weights)
    {
        var sum = weights.Select((weight, i) => (digits[i] - '0') * weight).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
