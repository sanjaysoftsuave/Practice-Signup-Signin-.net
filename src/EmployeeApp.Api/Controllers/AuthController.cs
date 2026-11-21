using System.Security.Claims;
using EmployeeApp.Api.Auth;
using EmployeeApp.Api.Contracts;
using EmployeeApp.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EmployeeApp.Api.Controllers;

/// <summary>
/// 10.1 Sign up and log in. These two endpoints must be open to everyone ([AllowAnonymous]):
/// you can't need a token to get a token.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Creates a new account with the "User" role (read-only access to employees).</summary>
    /// <response code="201">Created. The body has the account, without the password.</response>
    /// <response code="400">Invalid email, or the password is too weak.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("signup")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<UserResponse> SignUp(CredentialsRequest request)
    {
        try
        {
            var user = auth.SignUp(request.Email, request.Password);
            var body = new UserResponse(user.Id, user.Email, user.Role, user.CreatedOn);
            return CreatedAtAction(nameof(Me), null, body);   // Location: /api/auth/me
        }
        catch (ValidationException ex)
        {
            return Problem(title: "Invalid request", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (DuplicateEmailException ex)
        {
            return Problem(title: "Conflict", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Checks the email and password, and returns a JWT to send as "Authorization: Bearer &lt;token&gt;".</summary>
    /// <response code="200">The access token and when it expires.</response>
    /// <response code="400">The body is invalid (e.g. no email).</response>
    /// <response code="401">Wrong email or password. The message doesn't say which, on purpose.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> Login(CredentialsRequest request)
    {
        TokenResponse? token = auth.Login(request.Email, request.Password);
        return token is not null
            ? token
            : Problem(title: "Unauthorized", detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
    }

    /// <summary>Shows who the token belongs to. Needs a valid token: no token or an expired one gives 401.</summary>
    /// <response code="200">The claims read from the token.</response>
    /// <response code="401">No token, or the token is invalid or expired.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        // ControllerBase.User = the ClaimsPrincipal that the JwtBearer handler built from the token.
        // Nothing is loaded from the database: the token itself carries the id, email and role.
        return new CurrentUserResponse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            User.FindFirstValue(JwtRegisteredClaimNames.Email),
            User.FindAll("role").Select(c => c.Value).ToList());
    }
}
