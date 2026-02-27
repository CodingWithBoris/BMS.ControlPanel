using System.Windows.Controls;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class FactionSelectView : Page
{
    private readonly FactionSelectViewModel _viewModel;
    private List<Faction> _allFactions = new();

    public FactionSelectView(FactionSelectViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _ = _viewModel.LoadFactionsAsync().ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                _allFactions = _viewModel.Factions;
                FactionsListBox.ItemsSource = _allFactions;
            });
        });
    }

    private void OnFactionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (FactionsListBox.SelectedItem is Faction faction)
            _viewModel.SelectedFaction = faction;
    }

    private async void OnJoinFaction_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var password = OfficerPasswordBox.Password;
        await _viewModel.JoinFactionAsync(password);
    }

    private void OnShowCreateDialog_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.ShowCreateDialog();
    }

    private async void OnCreateFaction_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.OfficerPassword = CreateOfficerPwBox.Password;
        _viewModel.ViewPassword = CreateViewPwBox.Password;
        await _viewModel.CreateFactionAsync();
    }

    private void OnCancelCreate_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.CancelCreateDialog();
    }

    private void OnFactionSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = FactionSearchBox.Text.Trim();
        FactionsListBox.ItemsSource = string.IsNullOrEmpty(query)
            ? _allFactions
            : _allFactions.Where(f => f.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
