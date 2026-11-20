using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JBeamTool;

/// <summary>
/// JSON serialization context for trimming and AOT compatibility.
/// Provides compile-time serialization information for all types used with JsonSerializer.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(VerifyResponse))]
[JsonSerializable(typeof(VerifyError))]
[JsonSerializable(typeof(KeysResponse))]
[JsonSerializable(typeof(KeysError))]
[JsonSerializable(typeof(InfoResponse))]
[JsonSerializable(typeof(InfoError))]
[JsonSerializable(typeof(AstResponse))]
[JsonSerializable(typeof(AstError))]
internal partial class AppJsonSerializationContext : JsonSerializerContext
{
}

internal sealed record VerifyFileResult(string File, bool Ok, int Parts, bool Sanity, List<string> Warnings, int TranslationKeyCount);
internal sealed record VerifyResponse(bool Ok, bool Strict, int FileCount, int ResultCount, List<VerifyFileResult> Results, List<string> Warnings);
internal sealed record VerifyError(bool Ok, bool Strict, string Path, string Error, int? FileCount = null);
internal sealed record KeysResponse(bool Ok, string LanguageCode, string SourceDir, int FileCount, int MissingKeyCount, List<MissingKey> MissingKeys, bool Applied, List<string> UpdatedFiles, int UpdatedFileCount);
internal sealed record MissingKey(string Key, string Value);
internal sealed record KeysError(bool Ok, string SourceDir, string LanguageCode, string Error);
internal sealed record InfoResponse(bool Ok, string File, int Parts, bool Sanity, int WarningCount, List<string> Warnings, Dictionary<string, int> SectionCounts, List<InfoPart> PartsSummary);
internal sealed record InfoPart(string Id, string Name, string Value, string[] Authors, List<string> SlotType, string[] Slots);
internal sealed record InfoError(bool Ok, string File, string Error);
internal sealed record AstResponse(bool Ok, int FileCount, List<AstFile> Files);
internal sealed record AstFile(bool Ok, string File, int PartCount, bool Sanity, List<string> Warnings, JsonObject Root, List<AstPart> Parts);
internal sealed record AstPart(string Id, List<AstSection> Sections, JsonObject Node);
internal sealed record AstSection(string Type, string Identifier, List<AstProperty> Properties);
internal sealed record AstProperty(string Key, string ValueType);
internal sealed record AstError(bool Ok, string Path, string Error);
