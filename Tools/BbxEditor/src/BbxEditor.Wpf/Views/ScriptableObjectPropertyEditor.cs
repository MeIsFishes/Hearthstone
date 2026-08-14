using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using BbxEditor.Contracts;
using BbxEditor.Domain;

namespace BbxEditor.Wpf.Views;

public sealed class ScriptableObjectPropertyEditor : ContentControl
{
    public ScriptableObjectPropertyEditor() => DataContextChanged += Rebuild;

    private void Rebuild(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is not ScriptableObjectProperty property) { Content = null; return; }
        var binding = new Binding(nameof(ScriptableObjectProperty.Value))
        {
            Source = property,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        if (property.Type.Kind == EditorValueKind.Boolean || property.Type.Kind == EditorValueKind.Enum && property.Type.EnumValues.Count > 0)
        {
            var combo = new ComboBox
            {
                ItemsSource = property.Type.Kind == EditorValueKind.Boolean ? new[] { "true", "false" } : property.Type.EnumValues,
                IsEnabled = !property.IsReadOnly,
                ToolTip = property.Tooltip,
            };
            combo.SetBinding(Selector.SelectedItemProperty, binding);
            Content = combo;
            return;
        }
        var text = new TextBox
        {
            IsReadOnly = property.IsReadOnly,
            ToolTip = property.Tooltip,
            AcceptsReturn = property.Type.Kind == EditorValueKind.Array,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 30,
        };
        text.SetBinding(TextBox.TextProperty, binding);
        Content = text;
    }
}
