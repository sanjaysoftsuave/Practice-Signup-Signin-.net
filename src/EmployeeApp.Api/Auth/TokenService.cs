using System.Text;
using EmployeeApp.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeApp.Api.Auth;

/// <summary>The token sent back after a successful login.</summary>
public record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAt);

/// <summary>
/// 10.2 Creates a signed JWT: header.payload.signature (three Base64Url parts joined by dots).
///   header    {"alg":"HS256","typ":"JWT"}
///   payload   the claims: sub (user id), email, role, iss, aud, exp
///   signature HMAC-SHA256(header + payload, Key). Change one letter of the payload and the signature no longer matches.
/// The payload is only ENCODED, not encrypted: anyone can read it (try jwt.io), so never put secrets in it.
/// Singleton: it holds no per-request state.
/// </summary>
public class TokenService(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly JwtOptions _jwt = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenResponse CreateToken(AppUser user)
    {
        DateTime expiresAt = clock.GetUtcNow().UtcDateTime.AddMinutes(_jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,       // who made the token (this API)
            Audience = _jwt.Audience,   // who the token is for (this API's clients)
            Expires = expiresAt,        // "exp": after this, the API answers 401
            IssuedAt = clock.GetUtcNow().UtcDateTime,
            // Short claim names. Program.cs tells the JwtBearer handler that "role" holds the role.
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                ["role"] = user.Role,
            },
            SigningCredentials = new SigningCredentials(CreateSigningKey(_jwt.Key), SecurityAlgorithms.HmacSha256),
        };

        return new TokenResponse(_handler.CreateToken(descriptor), "Bearer", expiresAt);
    }

    /// <summary>The same key signs tokens here and checks them in the JwtBearer handler (Program.cs).</summary>
    public static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
