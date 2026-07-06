using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using HeicToJpg.Core;

namespace HeicToJpg.App.ViewModels;

public enum FileConversionStatus
{
    Pending,
    Converting,
    Success,
    Failed,
}

public partial class FileItemViewModel : ObservableObject
{
    public FileItemViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
    }

    public string FilePath { get; }

    public string FileName { get; }

    [ObservableProperty]
    private FileConversionStatus status = FileConversionStatus.Pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetailText))]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetailText))]
    private DecodePath? decodePath;

    [ObservableProperty]
    private string? outputPath;

    public string? DetailText => ErrorMessage ?? DecodePath switch
    {
        Core.DecodePath.LibHeif => "Decoded via libheif",
        Core.DecodePath.Wic => "Decoded via Windows codec (WIC)",
        _ => null,
    };
}
