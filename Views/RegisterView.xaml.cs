using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BMS.ControlPanel.Services;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class RegisterView : Page
{
    private readonly RegisterViewModel _viewModel;

    public RegisterView(RegisterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnRegister_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.Username = UsernameBox.Text.Trim();
        _viewModel.Password = PasswordBox.Password;
        _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        await _viewModel.RegisterAsync();
    }

    private void OnBackToLogin_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        _viewModel.BackToLoginClicked();
        e.Handled = true;
    }
}
