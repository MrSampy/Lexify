using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;

namespace Lexify.API.Tests.Blocks;

[Collection("Integration")]
public class BlocksControllerTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    [Fact]
    public async Task GetForeignBlock_ReturnsForbidden()
    {
        // User A creates a block
        var (clientA, _, _) = await factory.CreateAuthenticatedClientAsync();
        var createResp = await clientA.PostAsJsonAsync("/api/blocks",
            new { languageId = 1, title = "User A Block", description = (string?)null });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var blockIdJson = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var blockId = blockIdJson.GetString() ?? blockIdJson.GetRawText().Trim('"');

        // User B tries to access it
        var (clientB, _, _) = await factory.CreateAuthenticatedClientAsync();
        var resp = await clientB.GetAsync($"/api/blocks/{blockId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
