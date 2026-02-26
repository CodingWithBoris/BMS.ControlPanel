namespace BMS.ControlPanel.Models;

public class FactionOfficer
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DiscordTag { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime? LastBmsChange { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Shows Discord nickname if available, otherwise falls back to Username.
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(DiscordTag) ? DiscordTag : Username;
}
