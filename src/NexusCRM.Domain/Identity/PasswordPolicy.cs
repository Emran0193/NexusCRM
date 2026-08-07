namespace NexusCRM.Domain.Identity;

public sealed class PasswordPolicyOptions
{
    public int MinLength { get; set; } = 12;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireDigit { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; } = true;
}

public static class PasswordPolicy
{
    public static bool IsSatisfiedBy(string? password, PasswordPolicyOptions options)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < options.MinLength)
        {
            return false;
        }

        if (options.RequireUppercase && !password.Any(char.IsUpper))
        {
            return false;
        }

        if (options.RequireLowercase && !password.Any(char.IsLower))
        {
            return false;
        }

        if (options.RequireDigit && !password.Any(char.IsDigit))
        {
            return false;
        }

        if (options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            return false;
        }

        return true;
    }

    public static string Describe(PasswordPolicyOptions options)
    {
        var parts = new List<string> { $"at least {options.MinLength} characters" };
        if (options.RequireUppercase)
        {
            parts.Add("an uppercase letter");
        }

        if (options.RequireLowercase)
        {
            parts.Add("a lowercase letter");
        }

        if (options.RequireDigit)
        {
            parts.Add("a digit");
        }

        if (options.RequireNonAlphanumeric)
        {
            parts.Add("a non-alphanumeric character");
        }

        return "Password must contain " + string.Join(", ", parts) + ".";
    }
}
