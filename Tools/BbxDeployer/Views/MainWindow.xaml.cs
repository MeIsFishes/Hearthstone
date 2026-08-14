using System.ComponentModel;
using System.Windows;
using BbxDeployer.ViewModels;

namespace BbxDeployer.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private bool _isInitialized;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel.Owner = this;
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
        _isInitialized = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isInitialized)
        {
            Task.Run(_viewModel.SaveAsync).GetAwaiter().GetResult();
        }

        base.OnClosing(e);
    }
}
