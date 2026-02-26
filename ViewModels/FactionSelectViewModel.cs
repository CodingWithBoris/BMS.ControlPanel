using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class FactionSelectViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private List<Faction> _factions = new();
    private Faction? _selectedFaction;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;
    private bool _showCreateFactionDialog = false;
    private string _newFactionName = string.Empty;
    private string _officerPassword = string.Empty;
    private string _viewPassword = string.Empty;
    private string _defaultRoleName = "Member";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<Faction>? FactionJoined;

    public List<Faction> Factions
    {
        get => _factions;
        set { _factions = value; OnPropertyChanged(); }
    }

    public Faction? SelectedFaction
    {
        get => _selectedFaction;
        set { _selectedFaction = value; OnPropertyChanged(); }
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

    public bool ShowCreateFactionDialog
    {
        get => _showCreateFactionDialog;
        set { _showCreateFactionDialog = value; OnPropertyChanged(); }
    }

    public string NewFactionName
    {
        get => _newFactionName;
        set { _newFactionName = value; OnPropertyChanged(); }
    }

    public string OfficerPassword
    {
        get => _officerPassword;
        set { _officerPassword = value; OnPropertyChanged(); }
    }

    public string ViewPassword
    {
        get => _viewPassword;
        set { _viewPassword = value; OnPropertyChanged(); }
    }

    public string DefaultRoleName
    {
        get => _defaultRoleName;
        set { _defaultRoleName = value; OnPropertyChanged(); }
    }

    public FactionSelectViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadFactionsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading factions...";
        Factions = await _apiService.GetFactionsAsync();
        StatusMessage = Factions.Count > 0 ? $"Found {Factions.Count} faction(s)." : "No factions found. Create one!";
        IsLoading = false;
    }

    public async Task JoinFactionAsync(string? password = null)
    {
        if (SelectedFaction == null)
        {
            StatusMessage = "Please select a faction.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            StatusMessage = "Please enter the officer password.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Joining faction...";

        var (success, message) = await _apiService.JoinFactionAsync(SelectedFaction.Id, password);

        if (success)
        {
            StatusMessage = $"Joined faction '{SelectedFaction.Title}' successfully!";
            // Re-fetch the faction to get full data including roles
            var faction = await _apiService.GetFactionAsync(SelectedFaction.Id);
            FactionJoined?.Invoke(this, faction ?? SelectedFaction);
        }
        else
        {
            StatusMessage = message;
        }

        IsLoading = false;
    }

    public async Task CreateFactionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFactionName))
        {
            StatusMessage = "Please enter a faction name.";
            return;
        }
        if (string.IsNullOrWhiteSpace(OfficerPassword))
        {
            StatusMessage = "Please enter an officer password.";
            return;
        }
        if (string.IsNullOrWhiteSpace(ViewPassword))
        {
            StatusMessage = "Please enter a view password.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Creating faction...";

        var roles = new List<CreateRoleModel>
        {
            new() { Name = string.IsNullOrWhiteSpace(DefaultRoleName) ? "Member" : DefaultRoleName, IsDefault = true }
        };

        var faction = await _apiService.CreateFactionAsync(NewFactionName, OfficerPassword, ViewPassword, roles);

        if (faction != null)
        {
            StatusMessage = $"Faction '{faction.Title}' created successfully!";
            ShowCreateFactionDialog = false;
            FactionJoined?.Invoke(this, faction);
        }
        else
        {
            StatusMessage = "Failed to create faction. You may already own one.";
        }

        IsLoading = false;
    }

    public void ShowCreateDialog()
    {
        ShowCreateFactionDialog = true;
        NewFactionName = string.Empty;
        OfficerPassword = string.Empty;
        ViewPassword = string.Empty;
        DefaultRoleName = "Member";
        StatusMessage = string.Empty;
    }

    public void CancelCreateDialog()
    {
        ShowCreateFactionDialog = false;
        StatusMessage = string.Empty;
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
