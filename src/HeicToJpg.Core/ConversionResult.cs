namespace HeicToJpg.Core;

public enum DecodePath
{
    LibHeif,
    Wic,
}

public sealed record ConversionResult(string SourcePath, bool Success, string? OutputPath, string? ErrorMessage, DecodePath? DecodePath = null);
