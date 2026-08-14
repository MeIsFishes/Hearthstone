using System.Windows;
using BbxDeployer.Core;

namespace BbxDeployer.Views;

public partial class ProjectDialog : Window
{
    public ProjectDialog(ProjectContext project)
    {
        InitializeComponent();
        DisplayNameBox.Text = project.DisplayName;
        RepositoryRootBox.Text = project.RepositoryRoot;
        UnityProjectRootBox.Text = project.UnityProjectRoot;
    }

    public ProjectContext? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplayNameBox.Text)
            || string.IsNullOrWhiteSpace(RepositoryRootBox.Text)
            || string.IsNullOrWhiteSpace(UnityProjectRootBox.Text))
        {
            MessageBox.Show(
                this,
                "All fields are required.",
                "Edit Destination Project",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Result = new ProjectContext
        {
            DisplayName = DisplayNameBox.Text.Trim(),
            RepositoryRoot = RepositoryRootBox.Text.Trim(),
            UnityProjectRoot = UnityProjectRootBox.Text.Trim(),
            UnityEditorVersion = string.Empty
        };
        DialogResult = true;
    }
}
