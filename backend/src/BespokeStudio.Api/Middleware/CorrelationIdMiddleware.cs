using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Api.Middleware;

/// <summary>
/// Resolves a safe request correlation id, echoes it back on every response and
/// adds it to the logging scope. It never reads or logs request bodies, cookies,
/// tokens, Authorization headers, uploaded files, passwords or other secrets.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Request/response header carrying the correlation id.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Key used to store the resolved correlation id in <see cref="HttpContext.Items"/>.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    private const int MaxLength = 120;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request);

        context.Items[HttpContextItemKey] = correlationId;

        // Echo the id back immediately (readable synchronously) and re-apply on
        // response start so it survives responses whose headers were reset, for
        // example centralized Problem Details error handling.
        context.Response.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceIdentifier"] = context.TraceIdentifier,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value ?? string.Empty
        }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderName, out var values))
        {
            var candidate = values.ToString().Trim();
            if (IsValidCorrelationId(candidate))
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidCorrelationId(string value)
    {
        if (value.Length is 0 or > MaxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            var isSafe =
                character is >= 'A' and <= 'Z' ||
                character is >= 'a' and <= 'z' ||
                character is >= '0' and <= '9' ||
                character is '.' or '-' or '_' or ':';
            if (!isSafe)
            {
                return false;
            }
        }

        return true;
    }
}
