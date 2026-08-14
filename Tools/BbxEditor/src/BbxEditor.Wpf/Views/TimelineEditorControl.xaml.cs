using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using BbxEditor.Domain;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public partial class TimelineEditorControl : UserControl
{
    public TimelineEditorControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs args)
    {
        if (args.OldValue is TimelineDocumentViewModel oldViewModel) Unsubscribe(oldViewModel);
        if (args.NewValue is TimelineDocumentViewModel newViewModel) Subscribe(newViewModel);
    }

    private void Subscribe(TimelineDocumentViewModel viewModel)
    {
        viewModel.Items.CollectionChanged += OnItemsChanged;
        foreach (var item in viewModel.Items) item.PropertyChanged += OnItemPropertyChanged;
    }

    private void Unsubscribe(TimelineDocumentViewModel viewModel)
    {
        viewModel.Items.CollectionChanged -= OnItemsChanged;
        foreach (var item in viewModel.Items) item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null) foreach (TimelineItem item in args.OldItems) item.PropertyChanged -= OnItemPropertyChanged;
        if (args.NewItems is not null) foreach (TimelineItem item in args.NewItems) item.PropertyChanged += OnItemPropertyChanged;
        if (DataContext is TimelineDocumentViewModel viewModel) viewModel.MarkDirty();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (DataContext is TimelineDocumentViewModel viewModel) viewModel.MarkDirty();
    }
}
