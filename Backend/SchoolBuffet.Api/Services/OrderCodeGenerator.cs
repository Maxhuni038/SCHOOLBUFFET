using System.Security.Cryptography;

namespace SchoolBuffet.Api.Services;

public static class OrderCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int SuffixLength = 4;

    public static string Next(DateOnly day)
    {
        char[] suffix = new char[SuffixLength];
        for (int i = 0; i < SuffixLength; i++)
        {
            suffix[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "SB-{0:00}{1:00}{2:00}-{3}",
            day.Year % 100,
            day.Month,
            day.Day,
            new string(suffix));
    }
}
