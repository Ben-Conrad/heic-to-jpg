using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeicToJpg.Core;
using Microsoft.Win32;

namespace HeicToJpg.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string[] SupportedExtensions = { ".heic", ".heif" };

    private readonly BatchConverter _batchConverter = new();
    private CancellationTokenSource? _cts;

    public ObservableCollection<FileItemViewModel> Files { get; } = new();

    [ObservableProperty]
    private int jpegQuality = 90;

    [ObservableProperty]
    private bool useCustomOutputFolder;

    [ObservableProperty]
    private string? customOutputFolder;

    [ObservableProperty]
    private bool addSuffix;

    [ObservableProperty]
    private string suffix = "_converted";

    [ObservableProperty]
    private bool overwriteExisting;

    [ObservableProperty]
    private bool preserveGps = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
    private bool isConverting;

    [ObservableProperty]
    private int progressValue;

    [ObservableProperty]
    private int progressMax = 1;

    [ObservableProperty]
    private string statusMessage = "Drop HEIC files or folders here to get started.";

    public bool HasFiles => Files.Count > 0;

    public MainViewModel()
    {
        Files.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasFiles));
            ConvertCommand.NotifyCanExecuteChanged();
        };
    }

    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "HEIC images (*.heic;*.heif)|*.heic;*.heif",
            Multiselect = true,
        };

        if (dialog.ShowDialog() == true)
        {
            AddPaths(dialog.FileNames);
        }
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select output folder" };
        if (dialog.ShowDialog() == true)
        {
            CustomOutputFolder = dialog.FolderName;
            UseCustomOutputFolder = true;
        }
    }

    [RelayCommand]
    private void RemoveFile(FileItemViewModel? item)
    {
        if (item is not null)
        {
            Files.Remove(item);
        }
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
    }

    public void AddPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var found = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                AddPaths(found);
            }
            else if (File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                if (!Files.Any(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    Files.Add(new FileItemViewModel(path));
                }
            }
        }

        StatusMessage = $"{Files.Count} file(s) ready to convert.";
    }

    private bool CanConvert() => Files.Count > 0 && !IsConverting;

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync()
    {
        IsConverting = true;
        ProgressValue = 0;
        ProgressMax = Files.Count;
        _cts = new CancellationTokenSource();

        foreach (var file in Files)
        {
            file.Status = FileConversionStatus.Converting;
            file.ErrorMessage = null;
            file.DecodePath = null;
        }

        var options = new ConversionOptions
        {
            JpegQuality = JpegQuality,
            OutputLocation = UseCustomOutputFolder ? OutputLocationMode.CustomFolder : OutputLocationMode.SameAsSource,
            CustomOutputFolder = CustomOutputFolder,
            Naming = AddSuffix ? NamingMode.AddSuffix : NamingMode.KeepOriginalName,
            Suffix = Suffix,
            OverwriteExisting = OverwriteExisting,
            PreserveGps = PreserveGps,
        };

        var pathToItem = Files.ToDictionary(f => f.FilePath);

        var progress = new Progress<ConversionProgress>(p =>
        {
            if (pathToItem.TryGetValue(p.LatestResult.SourcePath, out var item))
            {
                item.Status = p.LatestResult.Success ? FileConversionStatus.Success : FileConversionStatus.Failed;
                item.ErrorMessage = p.LatestResult.ErrorMessage;
                item.DecodePath = p.LatestResult.DecodePath;
                item.OutputPath = p.LatestResult.OutputPath;
            }

            ProgressValue = p.Completed;
        });

        try
        {
            var results = await _batchConverter.ConvertAsync(
                Files.Select(f => f.FilePath).ToList(),
                options,
                progress,
                _cts.Token);

            var succeeded = results.Count(r => r.Success);
            StatusMessage = $"Converted {succeeded} of {results.Count} file(s).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Conversion cancelled.";
        }
        finally
        {
            IsConverting = false;
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelConvert()
    {
        _cts?.Cancel();
    }
}
