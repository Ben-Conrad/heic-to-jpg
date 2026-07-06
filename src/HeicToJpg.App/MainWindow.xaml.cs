using System.Reflection;
using System.Windows;
using HeicToJpg.App.ViewModels;

namespace HeicToJpg.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionText = version is null ? "unknown" : $"{version.Major}.{version.Minor}";

        MessageBox.Show(
            $"HEIC to JPG Converter\nVersion {versionText}\n© Ben Conrad",
            "About HEIC to JPG Converter",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            _viewModel.AddPaths(paths);
        }
    }
}
