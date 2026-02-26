using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class RolesView : Page
{
    private readonly RolesViewModel _viewModel;

    public RolesView(RolesViewModel viewModel, Faction faction)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _ = _viewModel.LoadRolesAsync(faction);
    }

    private void OnAddRole_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowCreateRoleDialog();
    }

    private void OnEditRole_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowEditRoleDialog();
    }

    private async void OnDeleteRole_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedRole == null)
        {
            _viewModel.StatusMessage = "Select a role first.";
            return;
        }

        var result = MessageBox.Show(
            $"Delete role \"{_viewModel.SelectedRole.Name}\"?\n\nNote: This will fail if the role is assigned to members or orders.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.DeleteRoleAsync();
        }
    }

    private async void OnCreateRole_Click(object sender, RoutedEventArgs e)
    {
        // Get password from PasswordBox (it doesn't support binding for security)
        _viewModel.NewRolePassword = NewRolePasswordBox.Password;
        await _viewModel.CreateRoleAsync();
    }

    private void OnCancelCreate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowCreateDialog = false;
    }

    private async void OnUpdateRole_Click(object sender, RoutedEventArgs e)
    {
        // Get password from PasswordBox
        _viewModel.EditRolePassword = EditRolePasswordBox.Password;
        await _viewModel.UpdateRoleAsync();
    }

    private void OnCancelEdit_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowEditDialog = false;
    }

    private void OnOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Close dialogs when clicking outside
        if (e.OriginalSource == sender)
        {
            _viewModel.ShowCreateDialog = false;
            _viewModel.ShowEditDialog = false;
        }
    }
}
