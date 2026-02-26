using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;
    private OAuthCallbackHandler? _oauthHandler;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LoginSucceeded;
    public event EventHandler? NavigateToRegister;

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
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

    public LoginViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    public async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Please enter username and password.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Logging in...";

        try
        {
            var result = await _apiService.LoginAsync(Username, Password);
            
            if (result?.Success == true && result.AccessToken != null)
            {
                await _authService.SaveTokensAsync(result);
                _apiService.SetAuthToken(result.AccessToken);
                
                // Test if the new token works immediately
                var authTest = await _apiService.TestAuthAsync();
                System.Diagnostics.Debug.WriteLine($"[Login] Auth test after login: {authTest.Success} - {authTest.Message}");
                
                if (!authTest.Success)
                {
                    StatusMessage = "Login succeeded but auth test failed - " + authTest.Message;
                    return;
                }
                
                StatusMessage = "Login successful!";
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else if (result == null)
            {
                StatusMessage = "Connection failed. Backend may be offline.";
                System.Diagnostics.Debug.WriteLine("API returned null - check backend server");
            }
            else
            {
                StatusMessage = result.Message ?? "Login failed. Check credentials.";
                System.Diagnostics.Debug.WriteLine($"Login failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Login Exception: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DiscordLoginAsync()
    {
        IsLoading = true;
        StatusMessage = "Initiating Discord login...";

        try
        {
            // Start listening for Discord callback
            _oauthHandler = new OAuthCallbackHandler();
            _oauthHandler.CallbackReceived += OnDiscordCallbackReceived;
            _oauthHandler.StartListening();

            // Get Discord OAuth URL
            string? oauthUrl = null;
            try
            {
                oauthUrl = await _apiService.GetDiscordOAuthUrlAsync();
            }
            catch (Exception apiEx)
            {
                StatusMessage = $"Failed to get Discord login URL: {apiEx.Message}";
                System.Diagnostics.Debug.WriteLine($"Discord API Error: {apiEx}");
                _oauthHandler?.StopListening();
                _oauthHandler?.Dispose();
                IsLoading = false;
                return;
            }

            if (string.IsNullOrEmpty(oauthUrl))
            {
                StatusMessage = "Failed to initiate Discord login. No OAuth URL received.";
                _oauthHandler?.StopListening();
                _oauthHandler?.Dispose();
                IsLoading = false;
                return;
            }

            // Open browser with Discord OAuth URL
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = oauthUrl,
                UseShellExecute = true
            });

            StatusMessage = "Please complete Discord login in your browser...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Discord Login Exception: {ex}");
            _oauthHandler?.StopListening();
            _oauthHandler?.Dispose();
            IsLoading = false;
        }
    }

    private async void OnDiscordCallbackReceived(object? sender, OAuthCallbackEventArgs args)
    {
        try
        {
            if (!args.Success)
            {
                StatusMessage = $"Discord login failed: {args.Error}";
                System.Diagnostics.Debug.WriteLine($"Discord OAuth error: {args.Error} - {args.ErrorDescription}");
            }
            else if (!string.IsNullOrEmpty(args.Code))
            {
                StatusMessage = "Completing login...";
                await CompleteDiscordLoginAsync(args.Code);
            }
        }
        finally
        {
            _oauthHandler?.StopListening();
            _oauthHandler?.Dispose();
            IsLoading = false;
        }
    }

    public async Task CompleteDiscordLoginAsync(string code)
    {
        IsLoading = true;
        StatusMessage = "Completing Discord login...";

        try
        {
            var result = await _apiService.DiscordCallbackAsync(code);
            
            if (result?.Success == true && result.AccessToken != null)
            {
                await _authService.SaveTokensAsync(result);
                _apiService.SetAuthToken(result.AccessToken);
                StatusMessage = "Discord login successful!";
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = result?.Message ?? "Discord login failed. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Discord login failed: {result?.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Discord Login Exception: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RegisterClicked()
    {
        NavigateToRegister?.Invoke(this, EventArgs.Empty);
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
