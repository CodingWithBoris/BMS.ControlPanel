using System.Windows;
using System.Windows.Controls;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class VcRosterView : Page
{
    private readonly VcRosterViewModel _viewModel;

    public VcRosterView(VcRosterViewModel viewModel, Faction faction)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _ = _viewModel.LoadRosterAsync(faction);
    }

    private async void OnSaveAll_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveAllDirtyAsync();
    }

    private async void OnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CurrentFaction != null)
            await _viewModel.LoadRosterAsync(_viewModel.CurrentFaction);
    }

}
