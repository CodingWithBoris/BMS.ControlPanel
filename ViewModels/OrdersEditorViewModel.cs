using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using BMS.ControlPanel.Models;
using BMS.ControlPanel.Services;
using BMS.Shared.Models;

namespace BMS.ControlPanel.ViewModels;

public class OrdersEditorViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly Faction _faction;
    private BmsOrder? _currentOrder;
    private ObservableCollection<BmsOrder> _orders = new();
    private ObservableCollection<Role> _roles = new();
    private ObservableCollection<EditableSection> _sections = new();
    private int _currentIndex = -1;
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;
    private bool _isPublished = false;
    private string _orderLabel = "No Orders";
    private Role? _selectedRole;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FactionId => _faction.Id;
    public string FactionTitle => _faction.Title;

    public BmsOrder? CurrentOrder
    {
        get => _currentOrder;
        set { _currentOrder = value; OnPropertyChanged(); }
    }

    public ObservableCollection<BmsOrder> Orders
    {
        get => _orders;
        set { _orders = value; OnPropertyChanged(); }
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

    public ObservableCollection<EditableSection> Sections
    {
        get => _sections;
        set { _sections = value; OnPropertyChanged(); }
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (_currentIndex != value)
            {
                _currentIndex = value;
                OnPropertyChanged();
                UpdateOrderLabel();
                if (value >= 0 && value < Orders.Count)
                {
                    // Set backing field and load sections BEFORE firing CurrentOrder PropertyChanged,
                    // so the UI rebuild triggered by PropertyChanged sees the correct sections.
                    _currentOrder = Orders[value];
                    SelectedRole = Roles.FirstOrDefault(r => r.Id == _currentOrder?.RoleId);
                    LoadSectionsFromOrder();
                    OnPropertyChanged(nameof(CurrentOrder));
                }
            }
        }
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

    public bool IsPublished
    {
        get => _isPublished;
        set { _isPublished = value; OnPropertyChanged(); }
    }

    public string OrderLabel
    {
        get => _orderLabel;
        set { _orderLabel = value; OnPropertyChanged(); }
    }

    public OrdersEditorViewModel(ApiService apiService, Faction faction)
    {
        _apiService = apiService;
        _faction = faction;
    }

    // ── Section management ──────────────────────────────

    private void LoadSectionsFromOrder()
    {
        Sections.Clear();
        if (CurrentOrder?.Sections != null)
        {
            foreach (var section in CurrentOrder.Sections.OrderBy(s => s.Index))
                Sections.Add(EditableSection.FromOrderSection(section));
        }
    }

    public EditableSection AddSection(string type)
    {
        var section = new EditableSection
        {
            Type = type,
            Index = Sections.Count,
            Title = $"Section {Sections.Count + 1}",
        };

        if (type == "poll")
        {
            section.PollOptionTexts.Add("Option 1");
            section.PollOptionTexts.Add("Option 2");
        }
        if (type == "checklist")
        {
            section.ChecklistItemTexts.Add("Item 1");
        }

        Sections.Add(section);
        return section;
    }

    public void RemoveSection(EditableSection section)
    {
        Sections.Remove(section);
        ReindexSections();
    }

    public void MoveSectionUp(EditableSection section)
    {
        var idx = Sections.IndexOf(section);
        if (idx > 0)
        {
            Sections.Move(idx, idx - 1);
            ReindexSections();
        }
    }

    public void MoveSectionDown(EditableSection section)
    {
        var idx = Sections.IndexOf(section);
        if (idx < Sections.Count - 1)
        {
            Sections.Move(idx, idx + 1);
            ReindexSections();
        }
    }

    private void ReindexSections()
    {
        for (int i = 0; i < Sections.Count; i++)
            Sections[i].Index = i;
    }

    public List<OrderSection> CollectSections()
    {
        ReindexSections();
        return Sections.Select(s => s.ToOrderSection()).ToList();
    }

    // ── Orders ──────────────────────────────────────────

    public async Task LoadOrdersAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading orders...";

        var roles = await _apiService.GetRolesAsync(_faction.Id);
        Roles = new ObservableCollection<Role>(roles);

        var orders = await _apiService.GetAllOrdersAsync(_faction.Id);
        Orders = new ObservableCollection<BmsOrder>(orders);

        if (Orders.Count > 0)
        {
            CurrentIndex = 0; // Sets CurrentOrder, loads sections, triggers UI rebuild
            IsPublished = CurrentOrder?.IsPublished ?? false;
        }
        else
        {
            CurrentIndex = -1;
            CurrentOrder = null;
        }

        UpdateOrderLabel();
        StatusMessage = Orders.Count > 0 ? $"Loaded {Orders.Count} order(s)." : "No orders yet. Click 'New' to create one.";
        IsLoading = false;
    }

    private void UpdateOrderLabel()
    {
        if (Orders.Count == 0)
            OrderLabel = "No Orders";
        else
            OrderLabel = $"Order {CurrentIndex + 1} of {Orders.Count}";
    }

    public void NextOrder()
    {
        if (Orders.Count > 0 && CurrentIndex < Orders.Count - 1)
            CurrentIndex++;
    }

    public void PreviousOrder()
    {
        if (CurrentIndex > 0)
            CurrentIndex--;
    }

    public async Task<BmsOrder?> CreateNewOrderAsync(string title, string content)
    {
        IsLoading = true;
        StatusMessage = "Creating new order...";

        int nextIndex = Orders.Count > 0 ? Orders.Max(o => o.OrderIndex) + 1 : 0;
        var order = await _apiService.CreateOrderAsync(
            _faction.Id,
            nextIndex,
            string.IsNullOrWhiteSpace(title) ? $"Order {nextIndex + 1}" : title,
            content,
            SelectedRole?.Id,
            new List<OrderSection>());

        if (order != null)
        {
            Orders.Add(order);
            CurrentIndex = Orders.Count - 1; // Sets CurrentOrder, loads sections, triggers UI rebuild
            IsPublished = order.IsPublished;
            StatusMessage = "New order created.";
        }
        else
        {
            StatusMessage = "Failed to create order.";
        }

        IsLoading = false;
        return order;
    }

    public async Task SaveDraftAsync(string title, string content)
    {
        if (CurrentOrder == null)
        {
            StatusMessage = "No order to save.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Saving draft...";

        var sections = CollectSections();
        var result = await _apiService.UpdateOrderAsync(
            _faction.Id,
            CurrentOrder.Id,
            title: title,
            content: content,
            isPublished: false,
            roleId: SelectedRole?.Id,
            sections: sections);

        if (result != null)
        {
            CurrentOrder.Title = result.Title;
            CurrentOrder.Content = result.Content;
            CurrentOrder.Sections = result.Sections;
            CurrentOrder.IsPublished = result.IsPublished;
            CurrentOrder.UpdatedAt = result.UpdatedAt;
            IsPublished = false;
            StatusMessage = "Draft saved successfully!";
        }
        else
        {
            StatusMessage = "Failed to save draft.";
        }

        IsLoading = false;
    }

    public async Task PublishOrderAsync(string title, string content)
    {
        if (CurrentOrder == null)
        {
            StatusMessage = "No order to publish.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Publishing order...";

        var sections = CollectSections();
        var updated = await _apiService.UpdateOrderAsync(
            _faction.Id,
            CurrentOrder.Id,
            title: title,
            content: content,
            roleId: SelectedRole?.Id,
            sections: sections);

        if (updated == null)
        {
            StatusMessage = "Failed to update order content.";
            IsLoading = false;
            return;
        }

        var result = await _apiService.PublishOrderAsync(_faction.Id, CurrentOrder.Id);
        if (result != null)
        {
            CurrentOrder.Title = title;
            CurrentOrder.Content = content;
            CurrentOrder.Sections = result.Sections;
            CurrentOrder.IsPublished = true;
            CurrentOrder.UpdatedAt = result.UpdatedAt;
            IsPublished = true;
            StatusMessage = "Order published successfully!";
        }
        else
        {
            StatusMessage = "Failed to publish order.";
        }

        IsLoading = false;
    }

    public async Task DeleteOrderAsync()
    {
        if (CurrentOrder == null || string.IsNullOrEmpty(CurrentOrder.Id))
        {
            StatusMessage = "Cannot delete this order.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Deleting order...";

        var success = await _apiService.DeleteOrderAsync(_faction.Id, CurrentOrder.Id);

        if (success)
        {
            Orders.Remove(CurrentOrder);
            if (Orders.Count > 0)
            {
                CurrentIndex = Math.Min(CurrentIndex, Orders.Count - 1);
                CurrentOrder = Orders[CurrentIndex];
                IsPublished = CurrentOrder.IsPublished;
                LoadSectionsFromOrder();
            }
            else
            {
                CurrentIndex = -1;
                CurrentOrder = null;
                IsPublished = false;
                Sections.Clear();
            }
            UpdateOrderLabel();
            StatusMessage = "Order deleted successfully!";
        }
        else
        {
            StatusMessage = "Failed to delete order.";
        }

        IsLoading = false;
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
