using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMS.Shared.Models;

namespace BMS.ControlPanel.ViewModels;

public class EditableSection : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private int _index;
    private string _type = "text";
    private string _title = string.Empty;
    private string? _content;
    private string? _imageUrl;
    private string? _videoUrl;
    private bool _allowMultipleChoice;

    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public int Index { get => _index; set { _index = value; OnPropertyChanged(); } }

    public string Type
    {
        get => _type;
        set { _type = value; OnPropertyChanged(); }
    }

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string? Content { get => _content; set { _content = value; OnPropertyChanged(); } }
    public string? ImageUrl { get => _imageUrl; set { _imageUrl = value; OnPropertyChanged(); } }
    public string? VideoUrl { get => _videoUrl; set { _videoUrl = value; OnPropertyChanged(); } }
    public bool AllowMultipleChoice { get => _allowMultipleChoice; set { _allowMultipleChoice = value; OnPropertyChanged(); } }

    public ObservableCollection<string> PollOptionTexts { get; set; } = new();
    public ObservableCollection<string> ChecklistItemTexts { get; set; } = new();

    public static EditableSection FromOrderSection(OrderSection section)
    {
        var editable = new EditableSection
        {
            Id = section.Id,
            Index = section.Index,
            Type = section.Type,
            Title = section.Title,
            Content = section.Content,
            ImageUrl = section.ImageUrl,
            VideoUrl = section.VideoUrl,
            AllowMultipleChoice = section.AllowMultipleChoice,
        };

        if (section.PollOptions != null)
            foreach (var opt in section.PollOptions)
                editable.PollOptionTexts.Add(opt.Text);

        if (section.ChecklistItems != null)
            foreach (var item in section.ChecklistItems)
                editable.ChecklistItemTexts.Add(item.Text);

        return editable;
    }

    public OrderSection ToOrderSection()
    {
        return new OrderSection
        {
            Id = Id,
            Index = Index,
            Type = Type,
            Title = Title,
            Content = Content,
            ImageUrl = ImageUrl,
            VideoUrl = VideoUrl,
            AllowMultipleChoice = Type == "poll" && AllowMultipleChoice,
            PollOptions = Type == "poll"
                ? PollOptionTexts.Select(t => new PollOption
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = t,
                }).ToList()
                : null,
            ChecklistItems = Type == "checklist"
                ? ChecklistItemTexts.Select(t => new ChecklistItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = t,
                }).ToList()
                : null,
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
