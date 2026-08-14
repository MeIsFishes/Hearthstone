using BbxDeployer.Core;
using BbxDeployer.Infrastructure;

namespace BbxDeployer.ViewModels;

public sealed class SyncItemViewModel : ObservableObject
{
    private SyncItem _model;

    public SyncItemViewModel(SyncItem model)
    {
        _model = model;
    }

    public event EventHandler? Changed;

    public SyncItem Model => _model;

    public string Id => _model.Id;

    public string DisplayName => _model.DisplayName;

    public bool IsBuiltIn => _model.IsBuiltIn;

    public string SourceDisplay => $"{FormatBase(_model.SourceBase)} / {_model.SourceRelativePath.Replace('\\', '/')}";

    public string TargetDisplay => $"{FormatBase(_model.TargetBase)} / {_model.TargetRelativePath.Replace('\\', '/')}";

    public string RelativeDirectory
    {
        get
        {
            var paths = SyncItemPathExpander.GetConfiguredPaths(_model);
            return paths.Count == 0
                ? "No paths configured"
                : paths.Count == 1
                ? paths[0].RelativePath
                : $"{paths[0].RelativePath} (+{paths.Count - 1})";
        }
    }

    public string ExclusionsDisplay
    {
        get
        {
            var manual = SyncItemPathExpander.GetConfiguredPaths(_model)
                .Sum(path => path.ManualExcludePatterns.Count);
            return $"{manual} rule{(manual == 1 ? string.Empty : "s")}";
        }
    }

    public void Replace(SyncItem model)
    {
        _model = model;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsBuiltIn));
        OnPropertyChanged(nameof(SourceDisplay));
        OnPropertyChanged(nameof(TargetDisplay));
        OnPropertyChanged(nameof(RelativeDirectory));
        OnPropertyChanged(nameof(ExclusionsDisplay));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatBase(PathBaseKind kind)
    {
        return kind == PathBaseKind.RepositoryRoot ? "Repository Root" : "Game Project";
    }
}
