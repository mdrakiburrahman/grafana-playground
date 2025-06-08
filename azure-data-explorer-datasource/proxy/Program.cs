using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var logger = app.Logger;

var endpointToQuery = Environment.GetEnvironmentVariable("KUSTO_ENDPOINT");
if (string.IsNullOrWhiteSpace(endpointToQuery))
{
    logger.LogError("KUSTO_ENDPOINT environment variable is not set.");
    return;
}

app.MapGet("/healthz", () => Results.Text("Healthy", "text/plain"));

app.MapPost("/v1/rest/mgmt", async context =>
{
    var routePath = context.Request.Path + context.Request.QueryString;
    var appId = await RequireAppIdAsync(context, logger);
    if (appId == null) return;

    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();
    await ProxyRequestAsync(requestBody, context, endpointToQuery, routePath, logger);
});

app.MapPost("/v1/rest/query", async context =>
{
    var routePath = context.Request.Path + context.Request.QueryString;
    var appId = await RequireAppIdAsync(context, logger);
    if (appId == null) return;

    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();
    await ProxyRequestAsync(requestBody, context, endpointToQuery, routePath, logger);
});

app.Run();

string? GetClaim(IHeaderDictionary headers, string claim, ILogger logger)
{
    var authHeader = headers["Authorization"].FirstOrDefault();
    if (
        !string.IsNullOrWhiteSpace(authHeader)
        && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
    )
    {
        var token = authHeader.Substring("Bearer ".Length).Trim();
        var handler = new JwtSecurityTokenHandler();

        if (handler.CanReadToken(token))
        {
            var jwtToken = handler.ReadJwtToken(token);
            var jwtClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == claim);
            if (jwtClaim != null)
            {
                return jwtClaim.Value;
            }
            else
            {
                logger.LogWarning("{claim} claim not found in the token.", claim);
            }
        }
        else
        {
            logger.LogWarning("Unable to read JWT token.");
        }
    }
    else
    {
        logger.LogWarning("Authorization header missing or not a Bearer token.");
    }

    return null;
}

async Task ProxyRequestAsync(
    string requestBody,
    HttpContext context,
    string endpointToQuery,
    string routePath,
    ILogger logger)
{
    var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

    using var httpClient = new HttpClient();

    var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Type",
        "Content-Length",
        "Content-Encoding",
        "Transfer-Encoding",
        "Host",
    };

    foreach (var header in context.Request.Headers)
    {
        if (restrictedHeaders.Contains(header.Key))
            continue;

        try
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                header.Key,
                header.Value.ToArray()
            );
        }
        catch
        {
            logger.LogError(
                "Failed to add header {HeaderKey} with value {HeaderValue} to HttpClient.",
                header.Key,
                string.Join(", ", header.Value)
            );
        }
    }

    var targetUrl = $"{endpointToQuery}{routePath}";
    var response = await httpClient.PostAsync(targetUrl, content);

    context.Response.StatusCode = (int)response.StatusCode;

    foreach (var header in response.Headers.Concat(response.Content.Headers))
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");

    var responseStream = await response.Content.ReadAsStreamAsync();
    await responseStream.CopyToAsync(context.Response.Body);
}

async Task<string?> RequireAppIdAsync(HttpContext context, ILogger logger)
{
    var appId = GetClaim(context.Request.Headers, "appid", logger);

    if (string.IsNullOrWhiteSpace(appId))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized: appid claim not found in the token.");
        return null;
    }

    logger.LogInformation("AppId: {AppId}", appId);
    return appId;
}