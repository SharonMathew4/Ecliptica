using System.Windows.Controls;
using Ecliptica.Engine.ViewModels;

namespace Ecliptica.App.Views;

public partial class ProjectPickerView : System.Windows.Controls.UserControl
{
    public ProjectPickerView()
    {
        InitializeComponent();
    }

    private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ProjectPickerViewModel vm && vm.NextCommand.CanExecute(null))
        {
            vm.NextCommand.Execute(null);
        }
    }
}
