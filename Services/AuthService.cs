using System.IO;
using System.Text.Json;
using BMS.ControlPanel.Models;

namespace BMS.ControlPanel.Services;

public class AuthService
{
    private readonly string _tokenPath;
    private string? _accessToken;
    private string? _refreshToken;
    private User? _currentUser;

    public User? CurrentUser => _currentUser;
    public string? AccessToken => _accessToken;

    public AuthService()
    {
        _tokenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BMS", "auth-tokens.json");
    }

    public async Task<bool> LoadTokensAsync()
    {
        try
        {
            if (File.Exists(_tokenPath))
            {
                var json = await File.ReadAllTextAsync(_tokenPath);
                var tokens = JsonSerializer.Deserialize<TokenData>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokens?.AccessToken != null)
                {
                    _accessToken = tokens.AccessToken;
                    _refreshToken = tokens.RefreshToken;
                    if (tokens.User != null) _currentUser = tokens.User;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token load error: {ex.Message}");
        }
        return false;
    }

    public async Task SaveTokensAsync(AuthResult authResult)
    {
        try
        {
            _accessToken = authResult.AccessToken;
            _refreshToken = authResult.RefreshToken;
            _currentUser = authResult.User;

            var dir = Path.GetDirectoryName(_tokenPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            var tokenData = new TokenData
            {
                AccessToken = authResult.AccessToken,
                RefreshToken = authResult.RefreshToken,
                User = authResult.User
            };

            var json = JsonSerializer.Serialize(tokenData);
            await File.WriteAllTextAsync(_tokenPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token save error: {ex.Message}");
        }
    }

    public void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _currentUser = null;

        try
        {
            if (File.Exists(_tokenPath)) File.Delete(_tokenPath);
        }
        catch { }
    }

    private class TokenData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public User? User { get; set; }
    }
}
