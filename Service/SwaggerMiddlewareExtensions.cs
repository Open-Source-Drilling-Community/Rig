using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Readers;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class SwaggerMiddlewareExtensions
{
    public static void UseCustomSwagger(this IApplicationBuilder app, string mergedDocumentJson, string relativePath)
    {
        app.Map(relativePath, builder =>
        {
            builder.Run(async context =>
            {
                // Dynamically compute scheme and host from request or reverse proxy headers.
                var req = context.Request;

                var scheme = req.Headers.ContainsKey("X-Forwarded-Host")
                    ? "https"
                    : req.Scheme;

                var host = req.Headers.ContainsKey("X-Forwarded-Host")
                    ? req.Headers["X-Forwarded-Host"].ToString()
                    : req.Host.Value;

                var pathBase = req.PathBase.HasValue ? req.PathBase.Value : string.Empty;

                context.Response.ContentType = "application/json";
                string outputString = RenderDocument(mergedDocumentJson, $"{scheme}://{host}{pathBase}");
                await context.Response.WriteAsync(outputString);
            });
        });
    }

    internal static string RenderDocument(string mergedDocumentJson, string serverUrl)
    {
        // Microsoft.OpenApi interprets profile versions such as "1.1.0" as dates
        // while round-tripping extensions. Edit the raw JSON tree so extension values
        // remain equivalent JSON values.
        if (JsonNode.Parse(mergedDocumentJson) is not JsonObject document)
            throw new InvalidDataException("The merged OpenAPI document is not a JSON object.");
        document["servers"] = new JsonArray(new JsonObject { ["url"] = serverUrl });
        return document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public static string ReadOpenApiDocumentJson(string filePath)
    {
        // Keep the previous startup validation, but serve the original JSON rather
        // than the lossy object-model serialization.
        using var stream = File.OpenRead(filePath);
        var reader = new OpenApiStreamReader();
        _ = reader.Read(stream, out var diagnostic);
        if (diagnostic.Errors.Count > 0)
        {
            Console.WriteLine("Warnings or errors while reading OpenAPI document:");
            foreach (var error in diagnostic.Errors)
                Console.WriteLine($"- {error.Message}");
        }

        return File.ReadAllText(filePath);
    }
}
