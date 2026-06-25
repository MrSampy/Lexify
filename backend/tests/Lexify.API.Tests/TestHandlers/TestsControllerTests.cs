using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;

namespace Lexify.API.Tests.TestHandlers;

[Collection("Integration")]
public class TestsControllerTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    private static readonly string[] OpenAnswerTypes = ["open_answer"];

    [Fact]
    public async Task GenerateTest_StatusIsGenerating()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        // Create block with enough words
        var createBlockResp = await client.PostAsJsonAsync("/api/blocks",
            new { languageId = 1, title = "Test Gen Block", description = (string?)null });
        Assert.Equal(HttpStatusCode.Created, createBlockResp.StatusCode);

        var blockIdJson = await createBlockResp.Content.ReadFromJsonAsync<JsonElement>();
        var blockId = blockIdJson.GetString() ?? blockIdJson.GetRawText().Trim('"');

        // Import 10 words so the word count requirement (>=5) is met
        var words = Enumerable.Range(1, 10).Select(i => new
        {
            term = $"term{i}", translation = $"trans{i}", wordType = "word",
            notes = (string?)null, exampleSentence = (string?)null,
            confidenceFlag = false, confidenceNote = (string?)null, sortOrder = i
        }).ToList();
        var importResp = await client.PostAsJsonAsync(
            $"/api/blocks/{blockId}/words/import", new { words });
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);

        // Generate test
        var genResp = await client.PostAsJsonAsync("/api/tests/generate", new
        {
            blockIds      = new[] { blockId },
            questionTypes = OpenAnswerTypes,
            questionCount = 5
        });
        Assert.Equal(HttpStatusCode.OK, genResp.StatusCode);

        var json = await genResp.Content.ReadFromJsonAsync<JsonElement>();
        var status = json.GetProperty("status").GetString();
        Assert.Equal("generating", status);
    }
}
