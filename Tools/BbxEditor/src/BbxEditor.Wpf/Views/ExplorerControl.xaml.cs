using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public partial class ExplorerControl : UserControl
{
    public ExplorerControl()
    {
        InitializeComponent();
    }

    private void ExplorerFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu })
        {
            menu.PlacementTarget = (Button)sender;
            menu.Items.Clear();
            if (DataContext is MainViewModel viewModel)
            {
                foreach (var filter in viewModel.ExplorerModFilters)
                    menu.Items.Add(CreateFilterMenuItem(filter, filter.SelectCommand));
                menu.Items.Add(new Separator());
                foreach (var filter in viewModel.ExplorerFileTypeFilters)
                    menu.Items.Add(CreateFilterMenuItem(filter, filter.SelectCommand));
            }
            menu.IsOpen = true;
        }
    }

    private static MenuItem CreateFilterMenuItem(object filter, System.Windows.Input.ICommand command)
    {
        var item = new MenuItem
        {
            DataContext = filter,
            Command = command,
            StaysOpenOnClick = true,
        };
        item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding("Header"));
        return item;
    }

    private void ExplorerFileList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox list ||
            ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) is not ListBoxItem)
        {
            return;
        }
        if (DataContext is MainViewModel viewModel && viewModel.OpenExplorerFileCommand.CanExecute(null))
        {
            viewModel.OpenExplorerFileCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ClearExplorerSearch_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.ExplorerSearchText = string.Empty;
        ExplorerSearchBox.Focus();
    }

    private void DesignPlanTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.SelectedDesignPlanFile = e.NewValue as DesignPlanFileViewModel;
    }

    private void DesignPlanTree_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject)?.DataContext is not DesignPlanFileViewModel file)
        {
            return;
        }
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedDesignPlanFile = file;
            if (viewModel.OpenDesignPlanCommand.CanExecute(null)) viewModel.OpenDesignPlanCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void DesignPlanSearchResults_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox list ||
            ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) is not ListBoxItem)
        {
            return;
        }
        if (DataContext is MainViewModel viewModel && viewModel.OpenDesignPlanCommand.CanExecute(null))
        {
            viewModel.OpenDesignPlanCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ClearDesignPlanSearch_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.DesignPlanSearchText = string.Empty;
        DesignPlanSearchBox.Focus();
    }

    private static T? FindVisualParent<T>(DependencyObject? element) where T : DependencyObject
    {
        for (var current = element; current is not null; current = GetParent(current))
            if (current is T match) return match;
        return null;
    }

    private static DependencyObject? GetParent(DependencyObject element)
    {
        if (element is FrameworkContentElement contentElement) return contentElement.Parent;
        return element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
    }
}
