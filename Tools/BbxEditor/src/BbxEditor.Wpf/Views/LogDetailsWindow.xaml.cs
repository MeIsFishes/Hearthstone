using System.Windows;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public partial class LogDetailsWindow : Window
{
    public LogDetailsWindow(ApplicationLog log)
    {
        InitializeComponent();
        DataContext = log;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
