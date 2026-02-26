using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;

namespace BMS.ControlPanel.ViewModels;

public class RolesViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private Faction? _currentFaction;
    private ObservableCollection<Role> _roles = new();
    private Role? _selectedRole;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;
    private bool _showCreateDialog = false;
    private bool _showEditDialog = false;
    private string _newRoleName = string.Empty;
    private string _newRolePassword = string.Empty;
    private bool _newRoleIsDefault = false;
    private string _editRoleName = string.Empty;
    private string _editRolePassword = string.Empty;
    private bool _editRoleIsDefault = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Faction? CurrentFaction
    {
        get => _currentFaction;
        set { _currentFaction = value; OnPropertyChanged(); }
    }

    public ObservableCollection<Role> Roles
    {
        get => _roles;
        set { _roles = value; OnPropertyChanged(); }
    }

    public Role? SelectedRole
    {
        get => _selectedRole;
        set { _selectedRole = value; OnPropertyChanged(); }
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

    public bool ShowCreateDialog
    {
        get => _showCreateDialog;
        set { _showCreateDialog = value; OnPropertyChanged(); }
    }

    public bool ShowEditDialog
    {
        get => _showEditDialog;
        set { _showEditDialog = value; OnPropertyChanged(); }
    }

    public string NewRoleName
    {
        get => _newRoleName;
        set { _newRoleName = value; OnPropertyChanged(); }
    }

    public string NewRolePassword
    {
        get => _newRolePassword;
        set { _newRolePassword = value; OnPropertyChanged(); }
    }

    public bool NewRoleIsDefault
    {
        get => _newRoleIsDefault;
        set { _newRoleIsDefault = value; OnPropertyChanged(); }
    }

    public string EditRoleName
    {
        get => _editRoleName;
        set { _editRoleName = value; OnPropertyChanged(); }
    }

    public string EditRolePassword
    {
        get => _editRolePassword;
        set { _editRolePassword = value; OnPropertyChanged(); }
    }

    public bool EditRoleIsDefault
    {
        get => _editRoleIsDefault;
        set { _editRoleIsDefault = value; OnPropertyChanged(); }
    }

    public RolesViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadRolesAsync(Faction faction)
    {
        CurrentFaction = faction;
        IsLoading = true;
        StatusMessage = "Loading roles...";

        var roles = await _apiService.GetRolesAsync(faction.Id);
        Roles = new ObservableCollection<Role>(roles);

        StatusMessage = $"Loaded {roles.Count} role(s).";
        IsLoading = false;
    }

    public void ShowCreateRoleDialog()
    {
        NewRoleName = string.Empty;
        NewRolePassword = string.Empty;
        NewRoleIsDefault = false;
        ShowCreateDialog = true;
    }

    public void ShowEditRoleDialog()
    {
        if (SelectedRole == null)
        {
            StatusMessage = "Please select a role to edit.";
            return;
        }

        EditRoleName = SelectedRole.Name;
        EditRolePassword = string.Empty; // Don't show existing password
        EditRoleIsDefault = SelectedRole.IsDefault;
        ShowEditDialog = true;
    }

    public async Task CreateRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoleName))
        {
            StatusMessage = "Role name is required.";
            return;
        }

        if (CurrentFaction == null) return;

        IsLoading = true;
        StatusMessage = "Creating role...";

        var role = await _apiService.CreateRoleAsync(
            CurrentFaction.Id,
            NewRoleName.Trim(),
            string.IsNullOrWhiteSpace(NewRolePassword) ? null : NewRolePassword,
            NewRoleIsDefault
        );

        if (role != null)
        {
            Roles.Add(role);
            ShowCreateDialog = false;
            StatusMessage = $"Role '{role.Name}' created successfully.";
        }
        else
        {
            StatusMessage = "Failed to create role. Role name may already exist.";
        }

        IsLoading = false;
    }

    public async Task UpdateRoleAsync()
    {
        if (SelectedRole == null || CurrentFaction == null) return;

        if (string.IsNullOrWhiteSpace(EditRoleName))
        {
            StatusMessage = "Role name is required.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Updating role...";

        var updatedRole = await _apiService.UpdateRoleAsync(
            CurrentFaction.Id,
            SelectedRole.Id,
            EditRoleName.Trim(),
            string.IsNullOrWhiteSpace(EditRolePassword) ? null : EditRolePassword,
            EditRoleIsDefault
        );

        if (updatedRole != null)
        {
            var index = Roles.IndexOf(SelectedRole);
            if (index >= 0)
            {
                Roles[index] = updatedRole;
                SelectedRole = updatedRole;
            }
            ShowEditDialog = false;
            StatusMessage = $"Role updated successfully.";
        }
        else
        {
            StatusMessage = "Failed to update role.";
        }

        IsLoading = false;
    }

    public async Task DeleteRoleAsync()
    {
        if (SelectedRole == null || CurrentFaction == null)
        {
            StatusMessage = "Please select a role to delete.";
            return;
        }

        if (SelectedRole.IsDefault)
        {
            StatusMessage = "Cannot delete the default role.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Deleting role...";

        var success = await _apiService.DeleteRoleAsync(CurrentFaction.Id, SelectedRole.Id);

        if (success)
        {
            Roles.Remove(SelectedRole);
            SelectedRole = null;
            StatusMessage = "Role deleted successfully.";
        }
        else
        {
            StatusMessage = "Failed to delete role. It may be assigned to members or orders.";
        }

        IsLoading = false;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
