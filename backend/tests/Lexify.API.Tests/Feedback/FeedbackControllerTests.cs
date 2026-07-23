using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;

namespace Lexify.API.Tests.Feedback;

[Collection("Integration")]
public class FeedbackControllerTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
    ];

    [Fact]
    public async Task Submit_Anonymous_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/feedback", BuildForm(new FeedbackForm()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submit_Valid_ReturnsIncreasingTicketNumbers_AndAppearsInMine()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var first = await SubmitAsync(client, new FeedbackForm { Subject = "First report" });
        var second = await SubmitAsync(client, new FeedbackForm { Subject = "Second report" });

        var firstNumber = first.GetProperty("ticketNumber").GetInt32();
        var secondNumber = second.GetProperty("ticketNumber").GetInt32();

        Assert.True(firstNumber >= 1000, $"Ticket numbers start at 1000, got {firstNumber}.");
        Assert.True(secondNumber > firstNumber);
        Assert.Equal($"LX-{firstNumber}", first.GetProperty("ticketCode").GetString());

        var mine = await client.GetFromJsonAsync<JsonElement>("/api/feedback/mine?page=1&pageSize=20");
        var subjects = mine.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("subject").GetString())
            .ToList();
        Assert.Contains("First report", subjects);
        Assert.Contains("Second report", subjects);
    }

    [Fact]
    public async Task Submit_ReviewWithoutRating_Returns422()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/feedback",
            BuildForm(new FeedbackForm { Type = "review", Rating = null }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Submit_RatingOutOfRange_Returns422()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/feedback",
            BuildForm(new FeedbackForm { Type = "review", Rating = 9 }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Submit_RatingOnNonReview_Returns422()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/feedback",
            BuildForm(new FeedbackForm { Type = "bug", Rating = 5 }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Submit_ShortSubject_Returns422()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/feedback",
            BuildForm(new FeedbackForm { Subject = "hi" }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutConsent_Returns422()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/feedback",
            BuildForm(new FeedbackForm { Consent = false }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Submit_ExecutableDisguisedAsPng_IsRejected()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        // Correct extension and declared content type, but the bytes are an MZ executable.
        var form = BuildForm(new FeedbackForm());
        AddFile(form, Encoding.ASCII.GetBytes("MZ\x90\0\x03\0\0\0\x04\0\0\0"), "screenshot.png", "image/png");

        var response = await client.PostAsync("/api/feedback", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Submit_FourthAttachment_IsRejected()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var form = BuildForm(new FeedbackForm());
        for (var i = 0; i < 4; i++)
            AddFile(form, PngBytes, $"shot{i}.png", "image/png");

        var response = await client.PostAsync("/api/feedback", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_OversizedAttachment_IsRejected()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var oversized = new byte[5 * 1024 * 1024 + 1];
        PngBytes.CopyTo(oversized, 0);

        var form = BuildForm(new FeedbackForm());
        AddFile(form, oversized, "huge.png", "image/png");

        var response = await client.PostAsync("/api/feedback", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_RejectNonAdmin()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var list = await client.GetAsync("/api/admin/feedback");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task Admin_CanReadAttachment_TriageStatus_AndItIsAudited()
    {
        var (userClient, _, _) = await factory.CreateAuthenticatedClientAsync();

        var form = BuildForm(new FeedbackForm { Subject = "Screenshot attached" });
        AddFile(form, PngBytes, "shot.png", "image/png");
        var submitResp = await userClient.PostAsync("/api/feedback", form);
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        var submitted = await submitResp.Content.ReadFromJsonAsync<JsonElement>();
        var feedbackId = submitted.GetProperty("id").GetString()!;

        var adminClient = await CreateAdminClientAsync();

        // Detail carries the attachment metadata, and the bytes come back verbatim.
        var detail = await adminClient.GetFromJsonAsync<JsonElement>($"/api/admin/feedback/{feedbackId}");
        var attachment = Assert.Single(detail.GetProperty("attachments").EnumerateArray());
        Assert.Equal("shot.png", attachment.GetProperty("fileName").GetString());
        Assert.Equal("image/png", attachment.GetProperty("contentType").GetString());

        var attachmentId = attachment.GetProperty("id").GetString();
        var download = await adminClient.GetAsync(
            $"/api/admin/feedback/{feedbackId}/attachments/{attachmentId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(PngBytes, await download.Content.ReadAsByteArrayAsync());

        // Triage: status flips and the change lands in the audit log.
        var update = await adminClient.PutAsJsonAsync(
            $"/api/admin/feedback/{feedbackId}/status",
            new { status = "resolved", adminNote = "Fixed in the next release." });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var afterUpdate = await adminClient.GetFromJsonAsync<JsonElement>($"/api/admin/feedback/{feedbackId}");
        Assert.Equal("resolved", afterUpdate.GetProperty("status").GetString());
        Assert.Equal("Fixed in the next release.", afterUpdate.GetProperty("adminNote").GetString());
        Assert.NotEqual(JsonValueKind.Null, afterUpdate.GetProperty("resolvedAt").ValueKind);

        var audit = await adminClient.GetFromJsonAsync<JsonElement>(
            "/api/admin/audit?action=update_feedback_status&page=1&pageSize=50");
        Assert.Contains(audit.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("targetId").GetString() == feedbackId);

        // The status filter now finds it, and the ticket code search does too.
        var resolved = await adminClient.GetFromJsonAsync<JsonElement>(
            "/api/admin/feedback?status=resolved&page=1&pageSize=50");
        Assert.Contains(resolved.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("id").GetString() == feedbackId);

        var ticketCode = detail.GetProperty("ticketCode").GetString();
        var bySearch = await adminClient.GetFromJsonAsync<JsonElement>(
            $"/api/admin/feedback?search={ticketCode}&page=1&pageSize=50");
        var found = Assert.Single(bySearch.GetProperty("items").EnumerateArray());
        Assert.Equal(feedbackId, found.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Mine_ReturnsOnlyOwnSubmissions()
    {
        var (alice, _, _) = await factory.CreateAuthenticatedClientAsync();
        var (bob, _, _) = await factory.CreateAuthenticatedClientAsync();

        await SubmitAsync(alice, new FeedbackForm { Subject = "Alice private report" });

        var bobsList = await bob.GetFromJsonAsync<JsonElement>("/api/feedback/mine?page=1&pageSize=50");

        Assert.DoesNotContain(bobsList.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("subject").GetString() == "Alice private report");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed class FeedbackForm
    {
        public string Type { get; init; } = "bug";
        public string? Category { get; init; } = "tests";
        public string Subject { get; init; } = "Something went wrong";
        public string Message { get; init; } = "The test runner froze halfway through the quiz.";
        public int? Rating { get; init; }
        public bool Consent { get; init; } = true;
    }

    private static MultipartFormDataContent BuildForm(FeedbackForm form)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(form.Type), "type" },
            { new StringContent(form.Subject), "subject" },
            { new StringContent(form.Message), "message" },
            { new StringContent(form.Consent.ToString()), "consent" }
        };

        if (form.Category is not null)
            content.Add(new StringContent(form.Category), "category");

        if (form.Rating is { } rating)
            content.Add(new StringContent(rating.ToString(CultureInfo.InvariantCulture)), "rating");

        return content;
    }

    private static void AddFile(
        MultipartFormDataContent form, byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "attachments", fileName);
    }

    private static async Task<JsonElement> SubmitAsync(HttpClient client, FeedbackForm form)
    {
        var response = await client.PostAsync("/api/feedback", BuildForm(form));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@lexify.test", password = "Admin1234!" });
        login.EnsureSuccessStatusCode();

        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());

        return client;
    }
}
