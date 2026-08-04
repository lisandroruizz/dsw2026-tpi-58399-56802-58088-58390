using System.Text.RegularExpressions;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class ValidationsExtensions
{
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
    public static bool IsEmailValid(this string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, EmailPattern);
    }

    public static bool HasLengthBetween(this string? value, int min, int max)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length >= min
            && value.Length <= max;
    }

    public static bool HasDigits(this long value,int minimum,int maximum)
    {
        int length = Math.Abs(value).ToString().Length;

        return length >= minimum && length <= maximum;
    }
}
