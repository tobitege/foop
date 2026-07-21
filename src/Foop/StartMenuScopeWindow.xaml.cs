using System.Windows;
using Foop.Models;

namespace Foop;

public partial class StartMenuScopeWindow : Window
{
    internal StartMenuShortcutScope? SelectedScope { get; private set; }

    public StartMenuScopeWindow()
    {
        InitializeComponent();
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        SelectedScope = AllUsersRadioButton.IsChecked == true
            ? StartMenuShortcutScope.AllUsers
            : StartMenuShortcutScope.CurrentUser;
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
