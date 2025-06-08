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

app.MapPost(
    "/v1/rest/query",
    async context =>
    {
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

        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{endpointToQuery}/v1/rest/query", content);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");

        var responseStream = await response.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(context.Response.Body);
    }
);

app.Run();
