using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMS.Shared.Models;

namespace BMS.ControlPanel.ViewModels;

public class EditableObjective : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private int _index;
    private string _text = string.Empty;

    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public int Index { get => _index; set { _index = value; OnPropertyChanged(); } }
    public string Text { get => _text; set { _text = value; OnPropertyChanged(); } }

    public static EditableObjective FromMissionObjective(MissionObjective objective) => new()
    {
        Id = objective.Id,
        Index = objective.Index,
        Text = objective.Text,
    };

    public MissionObjective ToMissionObjective() => new()
    {
        Id = Id,
        Index = Index,
        Text = Text,
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
