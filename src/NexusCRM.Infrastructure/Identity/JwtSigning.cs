using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NexusCRM.Infrastructure.Identity;

internal static class JwtSigning
{
    /// <summary>
    /// Builds a symmetric key with a stable KeyId so JwtBearer (JsonWebTokenHandler)
    /// can validate tokens that include a kid header (avoids IDX10517).
    /// </summary>
    public static SymmetricSecurityKey CreateSecurityKey(string signingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        var keyId = Convert.ToHexString(SHA256.HashData(keyBytes))[..16];
        return new SymmetricSecurityKey(keyBytes) { KeyId = keyId };
    }
}
