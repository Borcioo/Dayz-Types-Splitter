using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace TypesSplitter;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.CopyToClipboard = text => Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        if (Program.InitialFile is { } initial)
            _viewModel.LoadFile(initial);
    }

    private static string? FirstXmlFile(DragEventArgs e) =>
        e.Data.GetFiles()
            ?.Select(f => f.TryGetLocalPath())
            .FirstOrDefault(p => p is not null && p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

    private void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = FirstXmlFile(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (FirstXmlFile(e) is { } path)
            _viewModel.LoadFile(path);
    }

    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select types.xml",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("XML files") { Patterns = ["*.xml"] }],
        });

        if (files.Count == 1 && files[0].TryGetLocalPath() is { } path)
            _viewModel.LoadFile(path);
    }

    private async void OnPickOutput(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output folder",
            AllowMultiple = false,
        });

        if (folders.Count == 1 && folders[0].TryGetLocalPath() is { } path)
            _viewModel.OutputDirectory = path;
    }
}
