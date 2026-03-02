using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class UserSettingsViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private User _currentUser;
    private List<Faction> _userFactions;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<Faction>? SwitchToFaction;
    public event EventHandler? NavigateToFactionSelect;

    public User CurrentUser
    {
        get => _currentUser;
        set { _currentUser = value; OnPropertyChanged(); OnPropertyChanged(nameof(Username)); OnPropertyChanged(nameof(HasCreatedFaction)); OnPropertyChanged(nameof(CurrentUserId)); }
    }

    public string Username => _currentUser?.DisplayName ?? "Unknown";

    public string CurrentUserId => _currentUser?.Id ?? string.Empty;

    public bool HasCreatedFaction => !string.IsNullOrEmpty(_currentUser?.CreatedFactionId);

    public List<Faction> UserFactions
    {
        get => _userFactions;
        set { _userFactions = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public UserSettingsViewModel(ApiService apiService, User currentUser, List<Faction> userFactions)
    {
        _apiService = apiService;
        _currentUser = currentUser;
        _userFactions = new List<Faction>(userFactions);
    }

    public async Task RefreshFactionsAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing factions...";

        var factions = await _apiService.GetUserFactionsAsync();
        UserFactions = factions;

        // Also refresh user info
        var freshUser = await _apiService.GetCurrentUserAsync();
        if (freshUser != null)
            CurrentUser = freshUser;

        StatusMessage = $"You have access to {factions.Count} faction(s).";
        IsLoading = false;
    }

    public void SelectFaction(Faction faction)
    {
        SwitchToFaction?.Invoke(this, faction);
    }

    public void GoToJoinOrCreate()
    {
        NavigateToFactionSelect?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveDiscordServerIdAsync(Faction faction)
    {
        if (faction.OwnerId != CurrentUserId)
        {
            StatusMessage = "Only the faction owner can set the Discord server ID.";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Saving Discord server ID for '{faction.Title}'...";

        var updated = await _apiService.UpdateFactionAsync(faction.Id, discordServerId: faction.DiscordServerId);
        if (updated == null)
        {
            StatusMessage = "Failed to save Discord server ID.";
            IsLoading = false;
            return;
        }

        var idx = UserFactions.FindIndex(f => f.Id == faction.Id);
        if (idx >= 0)
            UserFactions[idx] = updated;

        OnPropertyChanged(nameof(UserFactions));
        StatusMessage = $"Discord server ID saved for '{updated.Title}'.";
        IsLoading = false;
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
