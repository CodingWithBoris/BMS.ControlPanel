using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BMS.ControlPanel.Services;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class LoginView : Page
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnLogin_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.Username = UsernameBox.Text.Trim();
        _viewModel.Password = PasswordBox.Password;
        await _viewModel.LoginAsync();
    }

    private async void OnDiscordLogin_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await _viewModel.DiscordLoginAsync();
    }

    private void OnCreateAccount_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.RegisterClicked();
    }
}
