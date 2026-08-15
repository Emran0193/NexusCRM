using Microsoft.AspNetCore.Identity;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Domain.Identity;

namespace NexusCRM.Infrastructure.Identity;

internal sealed class PasswordHasherService : IPasswordHasher
{
    private static readonly PasswordHasher<User> StaticHasher = new();
    private static readonly string DummyHash =
        StaticHasher.HashPassword(null!, "nexus-crm-timing-dummy-password");

    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public void PerformDummyVerification(string password)
    {
        // Intentionally ignore result; work approximates a failed real verification.
        _ = _hasher.VerifyHashedPassword(null!, DummyHash, password);
    }
}
