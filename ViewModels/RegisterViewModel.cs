using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class RegisterViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RegisterSucceeded;
    public event EventHandler? BackToLogin;

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

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { _confirmPassword = value; OnPropertyChanged(); }
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

    public RegisterViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    public async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Please fill in all fields.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            StatusMessage = "Passwords do not match.";
            return;
        }

        if (Password.Length < 6)
        {
            StatusMessage = "Password must be at least 6 characters.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Creating account...";

        try
        {
            var result = await _apiService.RegisterAsync(Username, Password);

            if (result?.Success == true && result.AccessToken != null)
            {
                await _authService.SaveTokensAsync(result);
                _apiService.SetAuthToken(result.AccessToken);
                StatusMessage = "Account created successfully!";
                RegisterSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else if (result == null)
            {
                StatusMessage = "Connection failed. Backend may be offline.";
                System.Diagnostics.Debug.WriteLine("API returned null - check backend server");
            }
            else
            {
                StatusMessage = result.Message ?? "Registration failed. Try different username.";
                System.Diagnostics.Debug.WriteLine($"Register failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Register Exception: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void BackToLoginClicked()
    {
        BackToLogin?.Invoke(this, EventArgs.Empty);
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
