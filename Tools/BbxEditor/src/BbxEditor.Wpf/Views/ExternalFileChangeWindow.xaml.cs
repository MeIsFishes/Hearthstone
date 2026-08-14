using System.IO;
using System.Windows;
using BbxEditor.Wpf.Services;

namespace BbxEditor.Wpf.Views;

public partial class ExternalFileChangeWindow : Window
{
    public ExternalFileChangeWindow(string filePath)
    {
        InitializeComponent();
        FilePath = filePath;
        DataContext = this;
    }

    public string FileName => Path.GetFileName(FilePath);
    public string FilePath { get; }
    public ExternalFileChangeChoice Choice { get; private set; } = ExternalFileChangeChoice.KeepLocal;

    private void KeepLocal_Click(object sender, RoutedEventArgs e)
    {
        Choice = ExternalFileChangeChoice.KeepLocal;
        DialogResult = true;
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        Choice = ExternalFileChangeChoice.Reload;
        DialogResult = true;
    }
}
