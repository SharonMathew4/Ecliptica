using System.Windows;

namespace Ecliptica.App.Views.Dialogs;

public partial class UnsavedChangesDialog : Window
{
    public string Result { get; private set; } = "cancel";

    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = "save";
        DialogResult = true;
        Close();
    }

    private void Discard_Click(object sender, RoutedEventArgs e)
    {
        Result = "discard";
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = "cancel";
        DialogResult = false;
        Close();
    }
}
