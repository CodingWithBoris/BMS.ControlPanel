using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace BMS.ControlPanel.Models;

public class VcRosterModel
{
    public string FactionId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public List<VcMember> Members { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class VcChannelGroup : INotifyPropertyChanged
{
    private string _teamName = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;

    public string TeamName
    {
        get => _teamName;
        set { _teamName = value; OnPropertyChanged(); }
    }

    public ObservableCollection<VcMember> Members { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class VcMember : INotifyPropertyChanged
{
    public string DiscordUserId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;

    private string _displayName = string.Empty;
    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
    }

    public string? AvatarUrl { get; set; }

    private string? _team;
    public string? Team
    {
        get => _team;
        set { _team = value; OnPropertyChanged(nameof(Team)); IsDirty = true; }
    }

    private string? _callsign;
    public string? Callsign
    {
        get => _callsign;
        set { _callsign = value; OnPropertyChanged(nameof(Callsign)); IsDirty = true; }
    }

    private string? _role;
    public string? Role
    {
        get => _role;
        set { _role = value; OnPropertyChanged(nameof(Role)); IsDirty = true; }
    }

    private bool _isHidden;
    public bool IsHidden
    {
        get => _isHidden;
        set { _isHidden = value; OnPropertyChanged(nameof(IsHidden)); IsDirty = true; }
    }

    public DateTime JoinedVcAt { get; set; }

    /// <summary>Tracks whether this member has unsaved local changes.</summary>
    public bool IsDirty { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
