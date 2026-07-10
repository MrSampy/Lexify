using System.Net;
using System.Text;
using System.Text.Json;
using Lexify.Application.AI.Dtos;
using Lexify.Infrastructure.AI;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lexify.API.Tests.AI;

/// <summary>
/// Verifies the exact wire shape of response_format requests OpenAiCompatibleClient sends — in
/// particular that "json_schema" is present (name/strict/schema) when the provider supports it, and
/// fully absent (not a literal null) when it doesn't, since llama.cpp-backed servers reject a
/// literal "json_schema": null on plain "json_object" requests.
/// </summary>
public class OpenAiRequestSerializationTests
{
    private static (OpenAiCompatibleClient Client, CapturingHandler Handler) CreateClient(
        AiProviderSettings settings, HttpResponseMessage response)
    {
        var handler = new CapturingHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:13305") };
        var client = new OpenAiCompatibleClient(httpClient, settings, Substitute.For<ILogger>());
        return (client, handler);
    }

    private static AiProviderSettings Settings(bool supportsJsonSchema) => new()
    {
        Name = "Test",
        BaseUrl = "http://localhost:13305",
        Model = "test-model",
        SupportsJsonSchema = supportsJsonSchema
    };

    [Fact]
    public async Task OpenEnrichStreamAsync_WithSchemaSupport_IncludesJsonSchemaResponseFormat()
    {
        var (client, handler) = CreateClient(
            Settings(supportsJsonSchema: true),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: [DONE]\n") });

        var lines = new[] { new ParsedImportLine(1, "dog - собака", "dog", "собака", [], false) };
        using var response = await client.OpenEnrichStreamAsync(lines, "English", "Russian", CancellationToken.None);

        var responseFormat = JsonDocument.Parse(handler.CapturedBody!).RootElement.GetProperty("response_format");

        Assert.Equal("json_schema", responseFormat.GetProperty("type").GetString());
        var jsonSchema = responseFormat.GetProperty("json_schema");
        Assert.Equal("enrich_words_result", jsonSchema.GetProperty("name").GetString());
        Assert.True(jsonSchema.GetProperty("strict").GetBoolean());
        Assert.Equal(JsonValueKind.Object, jsonSchema.GetProperty("schema").ValueKind);
    }

    [Fact]
    public async Task OpenEnrichStreamAsync_WithoutSchemaSupport_OmitsJsonSchemaFieldEntirely()
    {
        var (client, handler) = CreateClient(
            Settings(supportsJsonSchema: false),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: [DONE]\n") });

        var lines = new[] { new ParsedImportLine(1, "dog - собака", "dog", "собака", [], false) };
        using var response = await client.OpenEnrichStreamAsync(lines, "English", "Russian", CancellationToken.None);

        var responseFormat = JsonDocument.Parse(handler.CapturedBody!).RootElement.GetProperty("response_format");

        Assert.Equal("json_object", responseFormat.GetProperty("type").GetString());
        Assert.False(responseFormat.TryGetProperty("json_schema", out _));
    }

    [Fact]
    public async Task GenerateFakeDistractorsAsync_WithSchemaSupport_IncludesJsonSchemaResponseFormat()
    {
        const string responseJson =
            """{"choices":[{"message":{"role":"assistant","content":"{\"distractors\":[]}"}}]}""";

        var (client, handler) = CreateClient(
            Settings(supportsJsonSchema: true),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        await client.GenerateFakeDistractorsAsync("dog", 3, CancellationToken.None);

        var responseFormat = JsonDocument.Parse(handler.CapturedBody!).RootElement.GetProperty("response_format");

        Assert.Equal("json_schema", responseFormat.GetProperty("type").GetString());
        Assert.Equal("distractors_result", responseFormat.GetProperty("json_schema").GetProperty("name").GetString());
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string? CapturedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
