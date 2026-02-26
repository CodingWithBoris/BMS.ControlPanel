namespace BMS.ControlPanel.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? DiscordTag { get; set; }
    public string? CreatedFactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Shows Discord nickname if available, otherwise falls back to Username.
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(DiscordTag) ? DiscordTag : Username;
}
