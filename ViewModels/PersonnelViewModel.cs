using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class PersonnelViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private Faction? _currentFaction;
    private ObservableCollection<FactionOfficer> _officers = new();
    private FactionOfficer? _selectedOfficer;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;
    private bool _isOwner = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Faction? CurrentFaction
    {
        get => _currentFaction;
        set { _currentFaction = value; OnPropertyChanged(); }
    }

    public ObservableCollection<FactionOfficer> Officers
    {
        get => _officers;
        set { _officers = value; OnPropertyChanged(); }
    }

    public FactionOfficer? SelectedOfficer
    {
        get => _selectedOfficer;
        set { _selectedOfficer = value; OnPropertyChanged(); }
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

    public bool IsOwner
    {
        get => _isOwner;
        set { _isOwner = value; OnPropertyChanged(); }
    }

    public PersonnelViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadPersonnelAsync(Faction faction, bool isOwner)
    {
        CurrentFaction = faction;
        IsOwner = isOwner;
        IsLoading = true;
        StatusMessage = "Loading personnel...";

        var officers = await _apiService.GetPersonnelAsync(faction.Id);
        Officers = new ObservableCollection<FactionOfficer>(officers);

        StatusMessage = $"Loaded {officers.Count} officer(s).";
        IsLoading = false;
    }

    public async Task ToggleOfficerStatusAsync(FactionOfficer officer)
    {
        if (CurrentFaction == null || !IsOwner)
        {
            StatusMessage = "You do not have permission to modify officers.";
            return;
        }

        IsLoading = true;
        var success = await _apiService.UpdateOfficerAsync(CurrentFaction.Id, officer.UserId, isActive: !officer.IsActive);

        if (success)
        {
            officer.IsActive = !officer.IsActive;
            StatusMessage = officer.IsActive ? "Officer activated." : "Officer deactivated.";
        }
        else
        {
            StatusMessage = "Failed to update officer status.";
        }

        IsLoading = false;
    }

    public async Task RemoveOfficerAsync(FactionOfficer officer)
    {
        if (CurrentFaction == null || !IsOwner)
        {
            StatusMessage = "You do not have permission to remove officers.";
            return;
        }

        IsLoading = true;
        var success = await _apiService.RemoveOfficerAsync(CurrentFaction.Id, officer.UserId);

        if (success)
        {
            Officers.Remove(officer);
            StatusMessage = "Officer removed successfully.";
        }
        else
        {
            StatusMessage = "Failed to remove officer.";
        }

        IsLoading = false;
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
