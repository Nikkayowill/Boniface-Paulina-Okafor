using System.Text;

namespace Okafor_.NET.Services;

public static class NigerianPhoneNumber
{
    public static string NormalizeToE164(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new StringBuilder();
        foreach (var character in phone)
        {
            if (char.IsDigit(character))
            {
                digits.Append(character);
            }
        }

        var value = digits.ToString();
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (value.StartsWith("234", StringComparison.Ordinal) && value.Length == 13)
            return $"+{value}";

        if (value.StartsWith("0", StringComparison.Ordinal) && value.Length == 11)
            return $"+234{value[1..]}";

        if (value.Length == 10 && IsLikelyNigerianMobilePrefix(value[0]))
            return $"+234{value}";

        if (phone.Trim().StartsWith("+", StringComparison.Ordinal) &&
            value.Length is >= 8 and <= 15)
        {
            return $"+{value}";
        }

        return string.Empty;
    }

    public static string NormalizeForWhatsApp(string phone)
    {
        return NormalizeToE164(phone).TrimStart('+');
    }

    private static bool IsLikelyNigerianMobilePrefix(char firstDigit)
    {
        return firstDigit is '7' or '8' or '9';
    }
}
