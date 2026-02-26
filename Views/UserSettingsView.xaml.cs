using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class UserSettingsView : Page
{
    private readonly UserSettingsViewModel _viewModel;

    public UserSettingsView(UserSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshFactionsAsync();
    }

    private void OnSwitchFaction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Faction faction)
        {
            _viewModel.SelectFaction(faction);
        }
    }

    private void OnFactionItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Faction faction)
        {
            _viewModel.SelectFaction(faction);
        }
    }

    private void OnJoinOrCreate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.GoToJoinOrCreate();
    }
}
