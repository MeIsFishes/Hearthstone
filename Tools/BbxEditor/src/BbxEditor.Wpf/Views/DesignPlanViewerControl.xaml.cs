using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BbxEditor.Wpf.Services;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public partial class DesignPlanViewerControl : UserControl
{
    public DesignPlanViewerControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is not DesignPlanDocumentViewModel viewModel)
        {
            UpdateAssociatedDocumentButtons(null);
            return;
        }

        UpdateAssociatedDocumentButtons(viewModel);
        Browser.NavigateToString(MarkdownRenderService.RenderDocument(
            viewModel.DesignPlan.Markdown,
            viewModel.DesignPlan.FilePath ?? AppContext.BaseDirectory));
    }

    private void UpdateAssociatedDocumentButtons(DesignPlanDocumentViewModel? viewModel)
    {
        var hasPlan = !string.IsNullOrWhiteSpace(viewModel?.DesignPlan.PlanPath);
        var hasReview = !string.IsNullOrWhiteSpace(viewModel?.DesignPlan.ReviewPath);
        PlanButton.Visibility = hasPlan ? Visibility.Visible : Visibility.Collapsed;
        ReviewButton.Visibility = hasReview ? Visibility.Visible : Visibility.Collapsed;
        AssociatedDocumentsBar.Visibility = hasPlan || hasReview ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PlanButton_Click(object sender, RoutedEventArgs args) =>
        OpenAssociatedDocument("Plan", (DataContext as DesignPlanDocumentViewModel)?.DesignPlan.PlanPath);

    private void ReviewButton_Click(object sender, RoutedEventArgs args) =>
        OpenAssociatedDocument("Review", (DataContext as DesignPlanDocumentViewModel)?.DesignPlan.ReviewPath);

    private void OpenAssociatedDocument(string documentType, string? path)
    {
        if (DataContext is not DesignPlanDocumentViewModel viewModel || string.IsNullOrWhiteSpace(path)) return;
        viewModel.Owner.OpenAssociatedDesignPlan(path, $"{documentType}: {viewModel.DesignPlan.Title}");
    }

    private void Browser_Navigating(object sender, NavigatingCancelEventArgs args)
    {
        if (args.Uri is null || args.Uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase)) return;
        args.Cancel = true;
        var targetUri = MarkdownRenderService.TryDecodeDesignPlanLink(args.Uri, out var decodedUri)
            ? decodedUri!
            : args.Uri;
        if (DataContext is DesignPlanDocumentViewModel viewModel && viewModel.Owner.TryOpenDesignPlanLink(targetUri))
            return;

        try
        {
            _ = Process.Start(new ProcessStartInfo(targetUri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            if (DataContext is DesignPlanDocumentViewModel currentViewModel)
                currentViewModel.Owner.SetStatus("Could not open the document link: " + exception.Message, true);
        }
    }
}
