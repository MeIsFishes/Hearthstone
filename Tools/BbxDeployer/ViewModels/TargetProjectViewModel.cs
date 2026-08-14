using System.Collections.ObjectModel;
using BbxDeployer.Core;
using BbxDeployer.Infrastructure;

namespace BbxDeployer.ViewModels;

public sealed class TargetProjectViewModel(ProjectContext model) : ObservableObject
{
    private ProjectContext _model = model;
    private string _status = "Preview Required";
    private string _statusColor = "#64748B";
    private UnityEditorInstallation? _selectedUnityEditor;
    private bool _isUnityVersionSelectionVisible;
    private bool _isApplyingPreview;

    public event EventHandler? UnityEditorVersionChanged;

    public ProjectContext Model => _model;

    public string DisplayName => _model.DisplayName;

    public string RepositoryRoot => _model.RepositoryRoot;

    public string UnityProjectRoot => _model.UnityProjectRoot;

    public string RootDirectory => _model.RepositoryRoot;

    public ObservableCollection<string> RiskFiles { get; } = [];

    public ObservableCollection<UnityEditorInstallation> AvailableUnityEditors { get; } = [];

    public bool HasRiskFiles => RiskFiles.Count > 0;

    public bool IsUnityVersionSelectionVisible
    {
        get => _isUnityVersionSelectionVisible;
        private set => SetProperty(ref _isUnityVersionSelectionVisible, value);
    }

    public UnityEditorInstallation? SelectedUnityEditor
    {
        get => _selectedUnityEditor;
        set
        {
            if (!SetProperty(ref _selectedUnityEditor, value))
            {
                return;
            }

            if (!_isApplyingPreview)
            {
                _model.UnityEditorVersion = value?.Version ?? string.Empty;
                UnityEditorVersionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        private set => SetProperty(ref _statusColor, value);
    }

    public void ApplyPreview(
        TargetPreview? preview,
        IReadOnlyList<UnityEditorInstallation>? unityEditors = null)
    {
        RiskFiles.Clear();
        ApplyUnityEditors(preview, unityEditors ?? []);
        if (preview is null)
        {
            SetStatus("Unknown", "#64748B");
            OnPropertyChanged(nameof(HasRiskFiles));
            return;
        }

        if (preview.Errors.Count > 0)
        {
            SetStatus("Blocked", "#7C3AED");
            OnPropertyChanged(nameof(HasRiskFiles));
            return;
        }

        foreach (var risk in preview.RiskFiles)
        {
            RiskFiles.Add(
                $"{risk.TargetRelativePath}  |  Target: "
                + $"{risk.TargetLastWriteTimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}  |  Source: "
                + $"{risk.SourceLastWriteTimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        }

        switch (preview.Status)
        {
            case TargetSyncStatus.NewProject:
                SetStatus("New Project", "#2563EB");
                break;
            case TargetSyncStatus.WaitForSync:
                SetStatus("Wait for Sync", "#D97706");
                break;
            case TargetSyncStatus.Synchronized:
                SetStatus("Synchronized", "#16A34A");
                break;
            case TargetSyncStatus.Warning:
                SetStatus("Warning", "#DC2626");
                break;
        }

        OnPropertyChanged(nameof(HasRiskFiles));
    }

    public void InvalidatePreview()
    {
        RiskFiles.Clear();
        AvailableUnityEditors.Clear();
        _isApplyingPreview = true;
        SelectedUnityEditor = null;
        _isApplyingPreview = false;
        IsUnityVersionSelectionVisible = false;
        SetStatus("Preview Required", "#64748B");
        OnPropertyChanged(nameof(HasRiskFiles));
    }

    public void Replace(ProjectContext model)
    {
        _model = model;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(RepositoryRoot));
        OnPropertyChanged(nameof(UnityProjectRoot));
        OnPropertyChanged(nameof(RootDirectory));
    }

    private void ApplyUnityEditors(
        TargetPreview? preview,
        IReadOnlyList<UnityEditorInstallation> unityEditors)
    {
        _isApplyingPreview = true;
        try
        {
            AvailableUnityEditors.Clear();
            if (preview?.RequiresUnityProjectCreation != true)
            {
                SelectedUnityEditor = null;
                IsUnityVersionSelectionVisible = false;
                return;
            }

            foreach (var editor in unityEditors)
            {
                AvailableUnityEditors.Add(editor);
            }

            SelectedUnityEditor = AvailableUnityEditors.FirstOrDefault(editor =>
                editor.Version.Equals(
                    preview.Target.UnityEditorVersion,
                    StringComparison.OrdinalIgnoreCase));
            _model.UnityEditorVersion = SelectedUnityEditor?.Version ?? string.Empty;
            IsUnityVersionSelectionVisible = SelectedUnityEditor is not null;
        }
        finally
        {
            _isApplyingPreview = false;
        }
    }

    private void SetStatus(string status, string color)
    {
        Status = status;
        StatusColor = color;
    }
}
