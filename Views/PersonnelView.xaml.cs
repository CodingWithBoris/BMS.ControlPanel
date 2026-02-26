using System.Windows;
using System.Windows.Controls;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class PersonnelView : Page
{
    private readonly PersonnelViewModel _viewModel;

    public PersonnelView(PersonnelViewModel viewModel, Faction faction, bool isOwner)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _ = _viewModel.LoadPersonnelAsync(faction, isOwner);
    }

    private async void OnToggleActive_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedOfficer == null)
        {
            _viewModel.StatusMessage = "Select an officer first.";
            return;
        }

        await _viewModel.ToggleOfficerStatusAsync(_viewModel.SelectedOfficer);
    }

    private async void OnRemoveOfficer_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedOfficer == null)
        {
            _viewModel.StatusMessage = "Select an officer first.";
            return;
        }

        var result = MessageBox.Show(
            $"Remove officer \"{_viewModel.SelectedOfficer.DisplayName}\" from the faction?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.RemoveOfficerAsync(_viewModel.SelectedOfficer);
        }
    }
}
