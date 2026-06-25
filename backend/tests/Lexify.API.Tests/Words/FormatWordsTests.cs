using System.Net;
using System.Net.Http.Json;
using Lexify.API.Tests.Infrastructure;

namespace Lexify.API.Tests.Words;

[Collection("Integration")]
public class FormatWordsTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    [Fact]
    public async Task FormatWords_SSEEndpoint_ReceivesParsingStreamingDoneEvents()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/words/format");
        request.Content = JsonContent.Create(new
        {
            rawText        = "hello - привіт\nworld - світ",
            targetLanguage = "uk",
            nativeLanguage = "en"
        });

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream",
            response.Content.Headers.ContentType?.MediaType);

        // Read the SSE stream and collect event types
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);

        var eventTypes = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
                eventTypes.Add(line[7..].Trim());
        }

        Assert.Contains("parsing",   eventTypes);
        Assert.Contains("streaming", eventTypes);
        Assert.Contains("done",      eventTypes);
    }
}
