using System.Security.Cryptography;
using System.Text;
using NexusCRM.Application.Abstractions.Identity;

namespace NexusCRM.Infrastructure.Identity;

/// <summary>
/// RFC 6238 TOTP implementation for MFA (30s window, SHA1, 6 digits).
/// </summary>
internal sealed class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
        {
            return false;
        }

        var key = Base32Decode(secret);
        var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // Allow previous/current/next window for clock skew.
        for (var offset = -1; offset <= 1; offset++)
        {
            if (GenerateCode(key, window + offset) == code)
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateCode(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length + 4) / 5 * 8);
        int bitBuffer = 0, bitCount = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5)
            {
                output.Append(alphabet[(bitBuffer >> (bitCount - 5)) & 31]);
                bitCount -= 5;
            }
        }

        if (bitCount > 0)
        {
            output.Append(alphabet[(bitBuffer << (5 - bitCount)) & 31]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var cleaned = input.Trim().Replace("=", "", StringComparison.Ordinal).ToUpperInvariant();
        var bytes = new List<byte>();
        int bitBuffer = 0, bitCount = 0;

        foreach (var c in cleaned)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException("Invalid Base32 secret.");
            }

            bitBuffer = (bitBuffer << 5) | value;
            bitCount += 5;
            if (bitCount >= 8)
            {
                bytes.Add((byte)((bitBuffer >> (bitCount - 8)) & 0xFF));
                bitCount -= 8;
            }
        }

        return bytes.ToArray();
    }
}
