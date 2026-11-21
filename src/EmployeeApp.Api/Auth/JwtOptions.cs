using System.ComponentModel.DataAnnotations;

namespace EmployeeApp.Api.Auth;

/// <summary>
/// 7.7 Options pattern: the "Jwt" section of configuration, bound to a class.
/// Issuer, Audience and ExpiryMinutes live in appsettings.json. 10.4 Key is a SECRET, so it comes
/// from user-secrets (or an environment variable in production), never from a committed file.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";

    /// <summary>HMAC-SHA256 signing key. At least 32 characters (256 bits).</summary>
    [Required, MinLength(32)] public string Key { get; set; } = "";

    [Range(1, 24 * 60)] public int ExpiryMinutes { get; set; } = 60;
}
