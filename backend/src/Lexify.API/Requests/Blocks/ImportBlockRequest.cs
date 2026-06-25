using Microsoft.AspNetCore.Http;

namespace Lexify.API.Requests.Blocks;

public sealed record ImportBlockRequest(string Title, short LanguageId, IFormFile File);
