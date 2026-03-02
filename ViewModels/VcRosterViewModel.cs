using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class VcRosterViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private Faction? _currentFaction;
    private ObservableCollection<VcMember> _members = new();
    private ObservableCollection<VcChannelGroup> _channelGroups = new();
    private VcMember? _selectedMember;
    private string _statusMessage = string.Empty;
    private string _channelName = string.Empty;
    private bool _isLoading = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Faction? CurrentFaction
    {
        get => _currentFaction;
        set { _currentFaction = value; OnPropertyChanged(); }
    }

    public ObservableCollection<VcMember> Members
    {
        get => _members;
        set { _members = value; OnPropertyChanged(); }
    }

    public ObservableCollection<VcChannelGroup> ChannelGroups
    {
        get => _channelGroups;
        set { _channelGroups = value; OnPropertyChanged(); }
    }

    public VcMember? SelectedMember
    {
        get => _selectedMember;
        set { _selectedMember = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string ChannelName
    {
        get => _channelName;
        set { _channelName = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private bool _groupByTeamName = true;
    public bool GroupByTeamName
    {
        get => _groupByTeamName;
        set { _groupByTeamName = value; OnPropertyChanged(); }
    }

    public VcRosterViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadRosterAsync(Faction faction)
    {
        CurrentFaction = faction;
        IsLoading = true;
        StatusMessage = "Loading VC roster...";

        var rosters = await _apiService.GetVcRosterAsync(faction.Id);
        ChannelGroups.Clear();

        foreach (var roster in rosters.OrderBy(r => r.ChannelName))
        {
            var group = new VcChannelGroup
            {
                ChannelName = roster.ChannelName,
                ChannelId = roster.ChannelId
            };

            foreach (var member in roster.Members)
            {
                member.ChannelId = roster.ChannelId;
                member.ChannelName = roster.ChannelName;
                // Initialize IsDirty as false since we just loaded from the API
                member.IsDirty = false;
                group.Members.Add(member);
            }

            ChannelGroups.Add(group);
        }

        var totalMembers = ChannelGroups.SelectMany(g => g.Members).Count();
        StatusMessage = $"{totalMembers} member(s) in {ChannelGroups.Count} channel(s)";
        IsLoading = false;
    }

    public async Task SaveMemberAsync(VcMember member)
    {
        if (CurrentFaction == null) return;

        var success = await _apiService.UpdateVcMemberAsync(
            CurrentFaction.Id,
            member.DiscordUserId,
            team: member.Team,
            callsign: member.Callsign,
            role: member.Role,
            isHidden: member.IsHidden);

        if (success)
        {
            member.IsDirty = false;
            StatusMessage = $"Saved assignments for {member.DisplayName}";
        }
        else
        {
            StatusMessage = $"Failed to save {member.DisplayName}";
        }
    }

    public async Task SaveAllDirtyAsync()
    {
        if (CurrentFaction == null) return;

        IsLoading = true;
        int saved = 0;

        foreach (var group in ChannelGroups)
        {
            // Save channel team assignment
            if (!string.IsNullOrEmpty(group.TeamName))
            {
                var membersInChannel = group.Members.ToList();
                foreach (var member in membersInChannel)
                {
                    if (member.IsDirty || member.Team != group.TeamName)
                    {
                        var success = await _apiService.UpdateVcMemberAsync(
                            CurrentFaction.Id,
                            member.DiscordUserId,
                            team: group.TeamName,
                            callsign: member.Callsign,
                            role: member.Role,
                            isHidden: member.IsHidden);

                        if (success)
                        {
                            member.Team = group.TeamName;
                            member.IsDirty = false;
                            saved++;
                        }
                    }
                }
            }

            // Save individual member changes
            var dirty = group.Members.Where(m => m.IsDirty).ToList();
            foreach (var member in dirty)
            {
                var success = await _apiService.UpdateVcMemberAsync(
                    CurrentFaction.Id,
                    member.DiscordUserId,
                    team: member.Team,
                    callsign: member.Callsign,
                    role: member.Role,
                    isHidden: member.IsHidden);

                if (success)
                {
                    member.IsDirty = false;
                    saved++;
                }
            }
        }

        StatusMessage = $"Saved {saved} assignment(s).";
        IsLoading = false;
    }

    public async Task ToggleHiddenAsync(VcMember member)
    {
        if (CurrentFaction == null) return;

        var newHidden = !member.IsHidden;
        var success = await _apiService.UpdateVcMemberAsync(
            CurrentFaction.Id,
            member.DiscordUserId,
            isHidden: newHidden);

        if (success)
        {
            member.IsHidden = newHidden;
            member.IsDirty = false;

            // If hidden, remove from CP view (they won't come back from GET /vc-roster)
            if (newHidden)
            {
                Members.Remove(member);
                StatusMessage = $"{member.DisplayName} hidden from Control Panel (still visible on Overlay)";
            }
            else
            {
                StatusMessage = $"{member.DisplayName} is now visible";
            }
        }
        else
        {
            StatusMessage = $"Failed to update visibility for {member.DisplayName}";
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
