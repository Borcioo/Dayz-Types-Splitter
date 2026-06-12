using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypesSplitter.Core;

namespace TypesSplitter;

public partial class CategoryItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    public required string Name { get; init; }
    public required int Count { get; init; }

    public string Display => $"{Name}  ({Count})";
}

public partial class MainViewModel : ObservableObject
{
    private TypesDocument? _document;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string? _inputPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string? _outputDirectory;

    [ObservableProperty]
    private string _status = "Drop a types.xml here or click Open.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopySnippetCommand))]
    private string? _snippet;

    public ObservableCollection<CategoryItem> Categories { get; } = [];

    /// <summary>Set by the view; abstracts the Avalonia clipboard for testability.</summary>
    public Func<string, Task>? CopyToClipboard { get; set; }

    public void LoadFile(string path)
    {
        try
        {
            _document = TypesDocument.Load(path);
        }
        catch (Exception e)
        {
            Status = $"Failed to load: {e.Message}";
            return;
        }

        InputPath = path;
        Snippet = null;
        Categories.Clear();
        foreach (var (category, count) in _document.Categories)
            Categories.Add(new CategoryItem { Name = category, Count = count });

        // Default output next to the source file
        OutputDirectory ??= Path.GetDirectoryName(path);
        Status = $"Loaded {_document.TotalTypes} types in {Categories.Count} categories.";
    }

    [RelayCommand]
    private void SelectAll(bool select)
    {
        foreach (var c in Categories)
            c.IsSelected = select;
    }

    private bool CanSplit() => _document is not null && !string.IsNullOrEmpty(OutputDirectory);

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private void Split()
    {
        var selected = Categories.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        if (selected.Count == 0)
        {
            Status = "Nothing selected.";
            return;
        }

        try
        {
            var results = _document!.Split(OutputDirectory!, selected, s => Status = s);
            Snippet = TypesDocument.CfgEconomyCoreSnippet(results);
            Status = $"Done: {results.Sum(r => r.Count)} types written to {results.Count} files.";
        }
        catch (Exception e)
        {
            Status = $"Split failed: {e.Message}";
        }
    }

    private bool CanCopySnippet() => Snippet is not null;

    [RelayCommand(CanExecute = nameof(CanCopySnippet))]
    private async Task CopySnippet()
    {
        if (Snippet is null || CopyToClipboard is null)
            return;
        await CopyToClipboard(Snippet);
        Status = "cfgeconomycore.xml snippet copied to clipboard.";
    }
}
