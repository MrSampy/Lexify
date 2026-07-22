using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;

namespace Lexify.API.Tests.Conversations;

[Collection("Integration")]
public class ConversationsControllerTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    [Fact]
    public async Task FullFlow_Start_Send_End_PersistsTranscriptScoreAndListRow()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();
        var (conversationId, firstTerm) = await StartConversationAsync(client);

        // Send a learner message that uses one target word — the stub streams a fixed reply.
        var events = await SendMessageAsync(client, conversationId, $"Today I will use {firstTerm} in a sentence.");
        Assert.Contains(events, e => e.Event == "streaming");
        Assert.Equal("done", events[^1].Event);
        Assert.DoesNotContain(events, e => e.Event == "error");

        // Transcript persisted in order: opening, user turn, streamed reply.
        var detail = await GetJsonAsync(client, $"/api/conversations/{conversationId}");
        var messages = detail.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(3, messages.Count);
        Assert.Equal("assistant", messages[0].GetProperty("role").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("assistant", messages[2].GetProperty("role").GetString());
        Assert.Contains(firstTerm, messages[1].GetProperty("content").GetString());

        // End: the used word feeds SM-2 and the score is persisted.
        var endResp = await client.PostAsync($"/api/conversations/{conversationId}/end", null);
        Assert.Equal(HttpStatusCode.OK, endResp.StatusCode);
        var summary = await endResp.Content.ReadFromJsonAsync<JsonElement>();

        var usedWord = summary.GetProperty("words").EnumerateArray()
            .Single(w => w.GetProperty("term").GetString() == firstTerm);
        Assert.True(usedWord.GetProperty("used").GetBoolean());
        Assert.True(usedWord.GetProperty("intervalDays").ValueKind == JsonValueKind.Number);

        var score = summary.GetProperty("score");
        Assert.True(score.GetProperty("wordsUsed").GetInt32() >= 1);
        Assert.True(score.GetProperty("points").GetInt32() >= 10);

        // The list row carries the persisted score and a message COUNT (no bodies needed).
        var list = await GetJsonAsync(client, "/api/conversations?page=1&pageSize=20");
        var row = list.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetString() == conversationId);
        Assert.Equal("ended", row.GetProperty("status").GetString());
        Assert.Equal(3, row.GetProperty("messageCount").GetInt32());
        Assert.Equal(score.GetProperty("stars").GetInt32(), row.GetProperty("stars").GetInt32());
        Assert.Equal(score.GetProperty("points").GetInt32(), row.GetProperty("points").GetInt32());

        // And the stats endpoint reflects the finished session.
        var stats = await GetJsonAsync(client, "/api/stats/conversations");
        Assert.True(stats.GetProperty("totalSessions").GetInt32() >= 1);
        Assert.True(stats.GetProperty("wordsPractised").GetInt32() >= 1);
    }

    [Fact]
    public async Task SendMessage_ToEndedConversation_YieldsErrorEvent()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();
        var (conversationId, _) = await StartConversationAsync(client);

        (await client.PostAsync($"/api/conversations/{conversationId}/end", null)).EnsureSuccessStatusCode();

        var events = await SendMessageAsync(client, conversationId, "Hello again!");
        var error = Assert.Single(events, e => e.Event == "error");
        Assert.Contains("already ended", error.Data);
        Assert.DoesNotContain(events, e => e.Event == "done");
    }

    [Fact]
    public async Task SendMessage_Over2000Chars_YieldsErrorEvent()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();
        var (conversationId, _) = await StartConversationAsync(client);

        var events = await SendMessageAsync(client, conversationId, new string('a', 2001));
        var error = Assert.Single(events, e => e.Event == "error");
        Assert.Contains("too long", error.Data);
        Assert.DoesNotContain(events, e => e.Event == "streaming");
    }

    [Fact]
    public async Task End_Twice_SecondReturns400()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();
        var (conversationId, _) = await StartConversationAsync(client);

        var first = await client.PostAsync($"/api/conversations/{conversationId}/end", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsync($"/api/conversations/{conversationId}/end", null);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    /// <summary>Creates a block with words, starts a conversation, returns its id and one target term.</summary>
    private static async Task<(string ConversationId, string FirstTerm)> StartConversationAsync(HttpClient client)
    {
        var createBlockResp = await client.PostAsJsonAsync("/api/blocks",
            new { languageId = 1, title = "Chat words", description = (string?)null });
        Assert.Equal(HttpStatusCode.Created, createBlockResp.StatusCode);
        var blockIdJson = await createBlockResp.Content.ReadFromJsonAsync<JsonElement>();
        var blockId = blockIdJson.GetString() ?? blockIdJson.GetRawText().Trim('"');

        var words = Enumerable.Range(0, 6).Select(i => new
        {
            term = $"chatword{i}", translation = $"переклад{i}", wordType = "word",
            notes = (string?)null, exampleSentence = (string?)null,
            confidenceFlag = false, confidenceNote = (string?)null, sortOrder = i
        }).ToList();
        var importResp = await client.PostAsJsonAsync($"/api/blocks/{blockId}/words/import", new { words });
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);

        var startResp = await client.PostAsJsonAsync("/api/conversations",
            new { blockId, nativeLanguage = "English" });
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var start = await startResp.Content.ReadFromJsonAsync<JsonElement>();
        var conversationId = start.GetProperty("conversationId").GetString()!;
        var firstTerm = start.GetProperty("targetWords").EnumerateArray()
            .First().GetProperty("term").GetString()!;
        Assert.False(string.IsNullOrEmpty(start.GetProperty("openingMessage").GetString()));

        return (conversationId, firstTerm);
    }

    private static async Task<List<(string Event, string Data)>> SendMessageAsync(
        HttpClient client, string conversationId, string message)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/conversations/{conversationId}/messages");
        request.Content = JsonContent.Create(new { message, nativeLanguage = "English" });

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var events = new List<(string Event, string Data)>();
        string? pendingEvent = null;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
                pendingEvent = line[7..].Trim();
            else if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase) && pendingEvent != null)
            {
                events.Add((pendingEvent, line[6..]));
                pendingEvent = null;
            }
        }
        return events;
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var resp = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }
}
