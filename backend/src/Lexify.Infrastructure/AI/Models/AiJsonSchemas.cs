using System.Text.Json;

namespace Lexify.Infrastructure.AI.Models;

/// <summary>
/// JSON Schema documents sent as response_format.json_schema to grammar-constrain LLM decoding
/// (enforced by llama.cpp-backed OpenAI-compatible servers, e.g. Lemonade's GGUF backend).
/// Backends that don't support grammar constraints (e.g. Lemonade's Hybrid/OGA backend) receive
/// and silently ignore the field rather than reject the request — see
/// AiProviderSettings.SupportsJsonSchema for the escape hatch where sending it isn't safe at all.
/// </summary>
internal static class AiJsonSchemas
{
    public static readonly JsonElement EnrichWordsResult = Parse("""
        {
          "type": "object",
          "required": ["words"],
          "additionalProperties": false,
          "properties": {
            "suggestedTitle": { "type": ["string", "null"] },
            "words": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["id", "term", "translation", "wordType", "alternativeTranslations", "synonyms"],
                "additionalProperties": false,
                "properties": {
                  "id": { "type": "integer" },
                  "term": { "type": "string" },
                  "translation": { "type": "string" },
                  "wordType": { "enum": ["word", "phrase", "idiom", "expression"] },
                  "alternativeTranslations": { "type": "array", "items": { "type": "string" } },
                  "synonyms": { "type": "array", "items": { "type": "string" } },
                  "translationInTargetLanguage": { "type": ["boolean", "null"] },
                  "notes": { "type": ["string", "null"] },
                  "exampleSentence": { "type": ["string", "null"] },
                  "confidenceNote": { "type": ["string", "null"] }
                }
              }
            }
          }
        }
        """);

    public static readonly JsonElement FillSentencesResult = Parse("""
        {
          "type": "object",
          "required": ["sentences"],
          "additionalProperties": false,
          "properties": {
            "sentences": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["id", "sentence"],
                "additionalProperties": false,
                "properties": {
                  "id": { "type": "string" },
                  "sentence": { "type": "string" }
                }
              }
            }
          }
        }
        """);

    public static readonly JsonElement DefinitionsResult = Parse("""
        {
          "type": "object",
          "required": ["definitions"],
          "additionalProperties": false,
          "properties": {
            "definitions": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["id", "definition"],
                "additionalProperties": false,
                "properties": {
                  "id": { "type": "string" },
                  "definition": { "type": "string" }
                }
              }
            }
          }
        }
        """);

    public static readonly JsonElement DistractorsResult = Parse("""
        {
          "type": "object",
          "required": ["distractors"],
          "additionalProperties": false,
          "properties": {
            "distractors": { "type": "array", "items": { "type": "string" } }
          }
        }
        """);

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
