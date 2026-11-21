using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EmployeeApp.Api.Auth;

/// <summary>
/// 10.5 Tells the OpenAPI document that the API uses "Authorization: Bearer &lt;token&gt;".
/// Swagger UI then shows an "Authorize" button: paste the token from /api/auth/login once,
/// and every "Try it out" request sends it.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the accessToken from POST /api/auth/login (without the word Bearer).",
        };

        // Applies to the whole document. Sign up and login simply ignore it ([AllowAnonymous]).
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = [],
        });
        return Task.CompletedTask;
    }
}
