using System.Windows;
using System.Windows.Controls;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;
using BMS.ControlPanel.Views;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel;

public partial class MainWindow : Window
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private User? _currentUser;
    private Faction? _currentFaction;
    private List<Faction> _userFactions = new();

    public MainWindow()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _authService = new AuthService();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            if (await _authService.LoadTokensAsync())
            {
                var user = _authService.CurrentUser;
                if (user != null && _authService.AccessToken != null)
                {
                    _apiService.SetAuthToken(_authService.AccessToken);

                    // Test if authentication is working
                    var authTest = await _apiService.TestAuthAsync();
                    System.Diagnostics.Debug.WriteLine($"[App Init] Auth test result: {authTest.Success} - {authTest.Message}");

                    if (!authTest.Success)
                    {
                        System.Diagnostics.Debug.WriteLine("[App Init] Auth test failed - tokens may be expired, navigating to login");
                        NavigateToLogin();
                        return;
                    }

                    _currentUser = user;
                    await CheckFactionAndNavigate();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token load error: {ex.Message}");
        }

        NavigateToLogin();
    }

    // ──────────────────────────────────────────
    // Navigation Methods
    // ──────────────────────────────────────────
    private void NavigateToLogin()
    {
        TopBar.Visibility = Visibility.Collapsed;
        var loginViewModel = new LoginViewModel(_apiService, _authService);
        loginViewModel.LoginSucceeded += async (s, e) =>
        {
            _currentUser = _authService.CurrentUser;
            await CheckFactionAndNavigate();
        };
        loginViewModel.NavigateToRegister += (s, e) => NavigateToRegister();

        var loginView = new LoginView(loginViewModel);
        MainFrame.Navigate(loginView);
    }

    private void NavigateToRegister()
    {
        TopBar.Visibility = Visibility.Collapsed;
        var registerViewModel = new RegisterViewModel(_apiService, _authService);
        registerViewModel.RegisterSucceeded += async (s, e) =>
        {
            _currentUser = _authService.CurrentUser;
            await CheckFactionAndNavigate();
        };
        registerViewModel.BackToLogin += (s, e) => NavigateToLogin();

        var registerView = new RegisterView(registerViewModel);
        MainFrame.Navigate(registerView);
    }

    private async Task CheckFactionAndNavigate()
    {
        // Get user's faction memberships
        _userFactions = await _apiService.GetUserFactionsAsync();

        // Refresh current user info (for CreatedFactionId check)
        var freshUser = await _apiService.GetCurrentUserAsync();
        if (freshUser != null)
            _currentUser = freshUser;

        if (_userFactions.Count > 0)
        {
            // User has a faction — go to dashboard with first faction
            _currentFaction = _userFactions[0];
            NavigateToDashboard();
        }
        else
        {
            // No faction — show faction select
            NavigateToFactionSelect();
        }
    }

    private void NavigateToFactionSelect()
    {
        TopBar.Visibility = Visibility.Collapsed;
        var factionSelectViewModel = new FactionSelectViewModel(_apiService);
        factionSelectViewModel.FactionJoined += async (s, faction) =>
        {
            _currentFaction = faction;
            // Refresh user factions and user info
            _userFactions = await _apiService.GetUserFactionsAsync();
            var freshUser = await _apiService.GetCurrentUserAsync();
            if (freshUser != null)
                _currentUser = freshUser;
            NavigateToDashboard();
        };

        var factionSelectView = new FactionSelectView(factionSelectViewModel);
        MainFrame.Navigate(factionSelectView);
    }

    private void NavigateToDashboard()
    {
        if (_currentFaction == null) return;

        // Show top bar
        TopBar.Visibility = Visibility.Visible;
        FactionLabel.Text = $"Faction: {_currentFaction.Title}";

        // Show Roles and Personnel tabs only if user is the faction owner
        bool isOwner = _currentUser?.Id == _currentFaction.OwnerId;
        RolesTabBtn.Visibility = isOwner ? Visibility.Visible : Visibility.Collapsed;
        PersonnelTabBtn.Visibility = isOwner ? Visibility.Visible : Visibility.Collapsed;

        // Show Create Faction button only if user hasn't created one yet
        bool hasCreatedFaction = !string.IsNullOrEmpty(_currentUser?.CreatedFactionId);
        CreateFactionBtn.Visibility = hasCreatedFaction ? Visibility.Collapsed : Visibility.Visible;

        // Navigate to Orders tab by default
        ShowOrdersTab();
    }

    // ──────────────────────────────────────────
    // Tab Switching
    // ──────────────────────────────────────────
    private void SetTabActive(Button activeTab)
    {
        OrdersTabBtn.IsEnabled = true;
        RolesTabBtn.IsEnabled = true;
        PersonnelTabBtn.IsEnabled = true;
        SettingsTabBtn.IsEnabled = true;
        activeTab.IsEnabled = false;
    }

    private void ShowOrdersTab()
    {
        SetTabActive(OrdersTabBtn);

        var ordersViewModel = new OrdersEditorViewModel(_apiService, _currentFaction!);
        var ordersView = new OrdersEditorView(ordersViewModel);
        MainFrame.Navigate(ordersView);
    }

    private void ShowRolesTab()
    {
        SetTabActive(RolesTabBtn);

        var rolesViewModel = new RolesViewModel(_apiService);
        var rolesView = new RolesView(rolesViewModel, _currentFaction!);
        MainFrame.Navigate(rolesView);
    }

    private void ShowPersonnelTab()
    {
        SetTabActive(PersonnelTabBtn);

        bool isOwner = _currentUser?.Id == _currentFaction?.OwnerId;
        var personnelViewModel = new PersonnelViewModel(_apiService);
        var personnelView = new PersonnelView(personnelViewModel, _currentFaction!, isOwner);
        MainFrame.Navigate(personnelView);
    }

    private void ShowSettingsTab()
    {
        SetTabActive(SettingsTabBtn);

        var settingsViewModel = new UserSettingsViewModel(_apiService, _currentUser!, _userFactions);
        settingsViewModel.SwitchToFaction += (s, faction) =>
        {
            _currentFaction = faction;
            NavigateToDashboard();
        };
        settingsViewModel.NavigateToFactionSelect += (s, e) =>
        {
            NavigateToFactionSelect();
        };

        var settingsView = new UserSettingsView(settingsViewModel);
        MainFrame.Navigate(settingsView);
    }

    // ──────────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────────
    private void OnOrdersTab_Click(object sender, RoutedEventArgs e) => ShowOrdersTab();
    private void OnRolesTab_Click(object sender, RoutedEventArgs e) => ShowRolesTab();
    private void OnPersonnelTab_Click(object sender, RoutedEventArgs e) => ShowPersonnelTab();
    private void OnSettingsTab_Click(object sender, RoutedEventArgs e) => ShowSettingsTab();

    private async void OnSwitchFaction_Click(object sender, RoutedEventArgs e)
    {
        // Refresh the user's factions list
        _userFactions = await _apiService.GetUserFactionsAsync();

        if (_userFactions.Count == 0)
        {
            NoFactionsText.Visibility = Visibility.Visible;
            FactionPickerList.ItemsSource = null;
        }
        else
        {
            NoFactionsText.Visibility = Visibility.Collapsed;
            FactionPickerList.ItemsSource = _userFactions;
        }

        FactionPickerList.SelectedItem = null;
        FactionPickerPopup.IsOpen = true;
    }

    private void OnFactionPickerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (FactionPickerList.SelectedItem is Faction selectedFaction)
        {
            FactionPickerPopup.IsOpen = false;
            _currentFaction = selectedFaction;
            NavigateToDashboard();
        }
    }

    private void OnCreateFaction_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to faction select view (which has the create dialog)
        NavigateToFactionSelect();
    }

    private void OnLogout_Click(object sender, RoutedEventArgs e)
    {
        _authService.ClearTokens();
        _apiService.SetAuthToken(null);
        _currentUser = null;
        _currentFaction = null;
        _userFactions.Clear();
        NavigateToLogin();
    }
}
