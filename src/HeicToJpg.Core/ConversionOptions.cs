namespace HeicToJpg.Core;

public enum NamingMode
{
    KeepOriginalName,
    AddSuffix,
}

public enum OutputLocationMode
{
    SameAsSource,
    CustomFolder,
}

public sealed class ConversionOptions
{
    public int JpegQuality { get; init; } = 90;

    public OutputLocationMode OutputLocation { get; init; } = OutputLocationMode.SameAsSource;

    public string? CustomOutputFolder { get; init; }

    public NamingMode Naming { get; init; } = NamingMode.KeepOriginalName;

    public string Suffix { get; init; } = "_converted";

    public bool PreserveExif { get; init; } = true;

    public bool OverwriteExisting { get; init; } = false;
}
