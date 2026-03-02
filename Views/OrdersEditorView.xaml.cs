using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using BMS.ControlPanel.ViewModels;

namespace BMS.ControlPanel.Views;

public partial class OrdersEditorView : Page
{
    private readonly OrdersEditorViewModel _viewModel;
    private RichTextBox? _activeRichTextBox;

    // Track section cards for serialization
    private readonly Dictionary<string, SectionCard> _sectionCards = new();

    private class SectionCard
    {
        public EditableSection Section { get; set; } = null!;
        public Border Container { get; set; } = null!;
        public TextBox TitleBox { get; set; } = null!;
        public RichTextBox? ContentEditor { get; set; }
        public TextBox? ImageUrlBox { get; set; }
        public TextBox? VideoUrlBox { get; set; }
        public CheckBox? PollAllowMultipleChoice { get; set; }
        public StackPanel? PollPanel { get; set; }
        public StackPanel? ChecklistPanel { get; set; }
    }

    public OrdersEditorView(OrdersEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(OrdersEditorViewModel.CurrentOrder))
                LoadCurrentOrderToUI();
            if (e.PropertyName == nameof(OrdersEditorViewModel.IsPublished))
                UpdatePublishStatusLabel();
        };

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _viewModel.LoadOrdersAsync();
        LoadCurrentOrderToUI();
    }

    private void LoadCurrentOrderToUI()
    {
        if (_viewModel.CurrentOrder != null)
        {
            TitleBox.Text = _viewModel.CurrentOrder.Title ?? string.Empty;
            RebuildSectionsUI();
            RebuildObjectivesUI();
            UpdatePublishStatusLabel();
        }
        else
        {
            TitleBox.Text = string.Empty;
            SectionsPanel.Children.Clear();
            _sectionCards.Clear();
            _activeRichTextBox = null;
            ObjectivesPanel.Children.Clear();
            PublishStatusLabel.Text = "";
        }
    }

    private void UpdatePublishStatusLabel()
    {
        if (_viewModel.CurrentOrder == null)
        {
            PublishStatusLabel.Text = "";
            return;
        }
        PublishStatusLabel.Text = _viewModel.IsPublished ? "Published" : "Draft";
        PublishStatusLabel.Foreground = _viewModel.IsPublished
            ? new SolidColorBrush(Color.FromRgb(0x60, 0xFF, 0x60))
            : new SolidColorBrush(Color.FromRgb(0xB8, 0x86, 0x0B));
    }

    // ──── Section UI Building ────────────────────────────────────────

    private void RebuildSectionsUI()
    {
        SectionsPanel.Children.Clear();
        _sectionCards.Clear();
        _activeRichTextBox = null;

        foreach (var section in _viewModel.Sections)
        {
            var card = BuildSectionCard(section);
            SectionsPanel.Children.Add(card.Container);
            _sectionCards[section.Id] = card;
        }
    }

    private SectionCard BuildSectionCard(EditableSection section)
    {
        var card = new SectionCard { Section = section };

        // Container
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
        };
        card.Container = border;

        var stack = new StackPanel();
        border.Child = stack;

        // ── Header row: type label, title, move/delete buttons ──
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        stack.Children.Add(header);

        // Right-side buttons
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(btnPanel, Dock.Right);
        header.Children.Add(btnPanel);

        var upBtn = MakeSmallButton("\u25B2", () => { SyncSectionContent(card); _viewModel.MoveSectionUp(section); RebuildSectionsUI(); });
        var downBtn = MakeSmallButton("\u25BC", () => { SyncSectionContent(card); _viewModel.MoveSectionDown(section); RebuildSectionsUI(); });
        var delBtn = MakeSmallButton("\u2716", () => { _viewModel.RemoveSection(section); RebuildSectionsUI(); });

        btnPanel.Children.Add(upBtn);
        btnPanel.Children.Add(downBtn);
        btnPanel.Children.Add(delBtn);

        // Type label
        var typeColors = new Dictionary<string, Color>
        {
            ["text"]      = Color.FromRgb(0x1A, 0x30, 0x50),
            ["image"]     = Color.FromRgb(0x30, 0x18, 0x50),
            ["video"]     = Color.FromRgb(0x50, 0x18, 0x18),
            ["poll"]      = Color.FromRgb(0x3A, 0x28, 0x00),
            ["checklist"] = Color.FromRgb(0x0A, 0x2A, 0x0A),
            ["vcroster"]  = Color.FromRgb(0x2A, 0x20, 0x08),
        };
        var typeColor = typeColors.GetValueOrDefault(section.Type, Color.FromRgb(0x29, 0x80, 0xB9));
        var typeBadge = new Border
        {
            Background = new SolidColorBrush(typeColor),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = section.Type.ToUpperInvariant(),
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
            }
        };

        // Section title
        var titleBox = new TextBox
        {
            Text = section.Title,
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
        };
        card.TitleBox = titleBox;

        var leftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        leftPanel.Children.Add(typeBadge);
        leftPanel.Children.Add(titleBox);
        header.Children.Add(leftPanel);

        // ── Body based on type ──
        switch (section.Type)
        {
            case "text":
                BuildTextSectionBody(card, stack, section);
                break;
            case "image":
                BuildImageSectionBody(card, stack, section);
                break;
            case "video":
                BuildVideoSectionBody(card, stack, section);
                break;
            case "poll":
                BuildPollSectionBody(card, stack, section);
                break;
            case "checklist":
                BuildChecklistSectionBody(card, stack, section);
                break;
            case "vcroster":
                BuildVcRosterSectionBody(stack);
                break;
        }

        return card;
    }

    private void BuildTextSectionBody(SectionCard card, StackPanel parent, EditableSection section)
    {
        var rtb = new RichTextBox
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0A)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(8),
            FontSize = 14,
            MinHeight = 120,
            AcceptsTab = true,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        // Apply paragraph style
        var style = new Style(typeof(Paragraph));
        style.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 4)));
        rtb.Resources.Add(typeof(Paragraph), style);

        // Track active RichTextBox for formatting toolbar
        rtb.GotFocus += (s, e) => _activeRichTextBox = rtb;

        // Load existing content
        LoadContentToRichTextBox(rtb, section.Content);

        card.ContentEditor = rtb;

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0),
            Child = rtb,
        };
        parent.Children.Add(border);
    }

    private void BuildImageSectionBody(SectionCard card, StackPanel parent, EditableSection section)
    {
        var label = new TextBlock { Text = "Image URL:", Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) };
        parent.Children.Add(label);

        var urlBox = new TextBox
        {
            Text = section.ImageUrl ?? "",
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(8),
            FontSize = 13,
        };
        card.ImageUrlBox = urlBox;
        parent.Children.Add(urlBox);
    }

    private void BuildVideoSectionBody(SectionCard card, StackPanel parent, EditableSection section)
    {
        var label = new TextBlock { Text = "Video / GIF URL:", Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) };
        parent.Children.Add(label);

        var urlBox = new TextBox
        {
            Text = section.VideoUrl ?? "",
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(8),
            FontSize = 13,
        };
        card.VideoUrlBox = urlBox;
        parent.Children.Add(urlBox);

        var hint = new TextBlock
        {
            Text = "Supports .mp4, .webm, .gif and other video formats",
            Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x6A)),
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(0, 4, 0, 0),
        };
        parent.Children.Add(hint);
    }

    private void BuildPollSectionBody(SectionCard card, StackPanel parent, EditableSection section)
    {
        var label = new TextBlock { Text = "Poll Options:", Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) };
        parent.Children.Add(label);

        var multiChoiceCheck = new CheckBox
        {
            Content = "Allow multiple choice",
            IsChecked = section.AllowMultipleChoice,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 8),
            VerticalAlignment = VerticalAlignment.Center,
        };
        card.PollAllowMultipleChoice = multiChoiceCheck;
        parent.Children.Add(multiChoiceCheck);

        var pollPanel = new StackPanel();
        card.PollPanel = pollPanel;
        parent.Children.Add(pollPanel);

        foreach (var optText in section.PollOptionTexts)
            AddPollOptionRow(pollPanel, section, optText);

        var addBtn = new Button
        {
            Content = "+ Add Option",
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        ApplyButtonStyle(addBtn, useGold: true);
        addBtn.Click += (s, e) =>
        {
            section.PollOptionTexts.Add($"Option {section.PollOptionTexts.Count + 1}");
            AddPollOptionRow(pollPanel, section, section.PollOptionTexts.Last());
        };
        parent.Children.Add(addBtn);
    }

    private void AddPollOptionRow(StackPanel panel, EditableSection section, string text)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };

        var delBtn = MakeSmallButton("\u2716", () =>
        {
            var idx = panel.Children.IndexOf(row);
            if (idx >= 0 && idx < section.PollOptionTexts.Count)
            {
                section.PollOptionTexts.RemoveAt(idx);
                panel.Children.Remove(row);
            }
        });
        DockPanel.SetDock(delBtn, Dock.Right);
        row.Children.Add(delBtn);

        var tb = new TextBox
        {
            Text = text,
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 13,
        };
        row.Children.Add(tb);

        panel.Children.Add(row);
    }

    private void BuildChecklistSectionBody(SectionCard card, StackPanel parent, EditableSection section)
    {
        var label = new TextBlock { Text = "Checklist Items:", Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) };
        parent.Children.Add(label);

        var checkPanel = new StackPanel();
        card.ChecklistPanel = checkPanel;
        parent.Children.Add(checkPanel);

        foreach (var itemText in section.ChecklistItemTexts)
            AddChecklistItemRow(checkPanel, section, itemText);

        var addBtn = new Button
        {
            Content = "+ Add Item",
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        ApplyButtonStyle(addBtn, useGold: true);
        addBtn.Click += (s, e) =>
        {
            section.ChecklistItemTexts.Add($"Item {section.ChecklistItemTexts.Count + 1}");
            AddChecklistItemRow(checkPanel, section, section.ChecklistItemTexts.Last());
        };
        parent.Children.Add(addBtn);
    }

    private void BuildVcRosterSectionBody(StackPanel parent)
    {
        var hint = new TextBlock
        {
            Text = "This section will render live VC roster in the Overlay at this position.",
            Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
        };

        var note = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0A)),
            Padding = new Thickness(8),
            Child = hint,
        };

        parent.Children.Add(note);
    }

    private void AddChecklistItemRow(StackPanel panel, EditableSection section, string text)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };

        var delBtn = MakeSmallButton("\u2716", () =>
        {
            var idx = panel.Children.IndexOf(row);
            if (idx >= 0 && idx < section.ChecklistItemTexts.Count)
            {
                section.ChecklistItemTexts.RemoveAt(idx);
                panel.Children.Remove(row);
            }
        });
        DockPanel.SetDock(delBtn, Dock.Right);
        row.Children.Add(delBtn);

        var tb = new TextBox
        {
            Text = text,
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 13,
        };
        row.Children.Add(tb);

        panel.Children.Add(row);
    }

    private Button MakeSmallButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Width = 28,
            Height = 24,
            Margin = new Thickness(2, 0, 2, 0),
            FontSize = 11,
        };
        ApplyButtonStyle(btn, useGold: false);
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private void ApplyButtonStyle(Button button, bool useGold)
    {
        var resourceKey = useGold ? "CpGoldButtonStyle" : "CpDarkButtonStyle";
        if (TryFindResource(resourceKey) is Style style)
        {
            button.Style = style;
        }
    }

    // ──── Sync section UI data back to EditableSection models ────

    private void SyncAllSections()
    {
        foreach (var kvp in _sectionCards)
        {
            SyncSectionContent(kvp.Value);
        }
    }

    private void SyncSectionContent(SectionCard card)
    {
        card.Section.Title = card.TitleBox.Text;

        switch (card.Section.Type)
        {
            case "text":
                if (card.ContentEditor != null)
                    card.Section.Content = GetContentFromRichTextBox(card.ContentEditor);
                break;
            case "image":
                if (card.ImageUrlBox != null)
                    card.Section.ImageUrl = card.ImageUrlBox.Text;
                break;
            case "video":
                if (card.VideoUrlBox != null)
                    card.Section.VideoUrl = card.VideoUrlBox.Text;
                break;
            case "poll":
                if (card.PollAllowMultipleChoice != null)
                    card.Section.AllowMultipleChoice = card.PollAllowMultipleChoice.IsChecked == true;

                if (card.PollPanel != null)
                {
                    card.Section.PollOptionTexts.Clear();
                    foreach (DockPanel row in card.PollPanel.Children)
                    {
                        var tb = row.Children.OfType<TextBox>().FirstOrDefault();
                        if (tb != null) card.Section.PollOptionTexts.Add(tb.Text);
                    }
                }
                break;
            case "checklist":
                if (card.ChecklistPanel != null)
                {
                    card.Section.ChecklistItemTexts.Clear();
                    foreach (DockPanel row in card.ChecklistPanel.Children)
                    {
                        var tb = row.Children.OfType<TextBox>().FirstOrDefault();
                        if (tb != null) card.Section.ChecklistItemTexts.Add(tb.Text);
                    }
                }
                break;
        }
    }

    // ──── Content serialization ────────────────────────────────────

    private string GetContentFromRichTextBox(RichTextBox rtb)
    {
        try
        {
            var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            using var stream = new MemoryStream();
            range.Save(stream, DataFormats.Xaml);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            return range.Text;
        }
    }

    private void LoadContentToRichTextBox(RichTextBox rtb, string? content)
    {
        rtb.Document.Blocks.Clear();

        if (string.IsNullOrWhiteSpace(content))
        {
            rtb.Document.Blocks.Add(new Paragraph());
            return;
        }

        try
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            range.Load(stream, DataFormats.Xaml);
        }
        catch
        {
            rtb.Document.Blocks.Clear();
            rtb.Document.Blocks.Add(new Paragraph(new Run(content)));
        }
    }

    // ──── Add Section buttons ──────────────────────────────────────

    private void OnAddTextSection_Click(object sender, RoutedEventArgs e) => AddSectionAndRebuild("text");
    private void OnAddImageSection_Click(object sender, RoutedEventArgs e) => AddSectionAndRebuild("image");
    private void OnAddVideoSection_Click(object sender, RoutedEventArgs e) => AddSectionAndRebuild("video");
    private void OnAddPollSection_Click(object sender, RoutedEventArgs e) => AddSectionAndRebuild("poll");
    private void OnAddChecklistSection_Click(object sender, RoutedEventArgs e) => AddSectionAndRebuild("checklist");
    private void OnAddVcRosterSection_Click(object sender, RoutedEventArgs e) => AddSectionAndRebuild("vcroster");

    private void AddSectionAndRebuild(string type)
    {
        SyncAllSections();
        _viewModel.AddSection(type);
        RebuildSectionsUI();
    }

    // ──── Navigation ──────────────────────────────────────────────

    private void OnPrevOrder_Click(object sender, RoutedEventArgs e) => _viewModel.PreviousOrder();
    private void OnNextOrder_Click(object sender, RoutedEventArgs e) => _viewModel.NextOrder();

    private async void OnNewOrder_Click(object sender, RoutedEventArgs e)
    {
        var order = await _viewModel.CreateNewOrderAsync("New Order", "");
        if (order != null)
            LoadCurrentOrderToUI();
    }

    private async void OnDeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CurrentOrder == null) return;

        var result = MessageBox.Show(
            $"Delete order \"{_viewModel.CurrentOrder.Title}\"?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.DeleteOrderAsync();
            LoadCurrentOrderToUI();
        }
    }

    // ──── Formatting (applies to active text section RichTextBox) ──

    private void OnBold_Click(object sender, RoutedEventArgs e)
    {
        if (_activeRichTextBox == null) return;
        var sel = _activeRichTextBox.Selection;
        if (sel.IsEmpty) return;
        var cur = sel.GetPropertyValue(TextElement.FontWeightProperty);
        sel.ApplyPropertyValue(TextElement.FontWeightProperty,
            (cur is FontWeight fw && fw == FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold);
        _activeRichTextBox.Focus();
    }

    private void OnItalic_Click(object sender, RoutedEventArgs e)
    {
        if (_activeRichTextBox == null) return;
        var sel = _activeRichTextBox.Selection;
        if (sel.IsEmpty) return;
        var cur = sel.GetPropertyValue(TextElement.FontStyleProperty);
        sel.ApplyPropertyValue(TextElement.FontStyleProperty,
            (cur is FontStyle fs && fs == FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic);
        _activeRichTextBox.Focus();
    }

    private void OnUnderline_Click(object sender, RoutedEventArgs e)
    {
        if (_activeRichTextBox == null) return;
        var sel = _activeRichTextBox.Selection;
        if (sel.IsEmpty) return;
        var cur = sel.GetPropertyValue(Inline.TextDecorationsProperty);
        if (cur is TextDecorationCollection tdc && tdc.Contains(TextDecorations.Underline[0]))
            sel.ApplyPropertyValue(Inline.TextDecorationsProperty, new TextDecorationCollection());
        else
            sel.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
        _activeRichTextBox.Focus();
    }

    private void OnStrikethrough_Click(object sender, RoutedEventArgs e)
    {
        if (_activeRichTextBox == null) return;
        var sel = _activeRichTextBox.Selection;
        if (sel.IsEmpty) return;
        var cur = sel.GetPropertyValue(Inline.TextDecorationsProperty);
        if (cur is TextDecorationCollection tdc && tdc.Contains(TextDecorations.Strikethrough[0]))
            sel.ApplyPropertyValue(Inline.TextDecorationsProperty, new TextDecorationCollection());
        else
            sel.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
        _activeRichTextBox.Focus();
    }

    private void OnBulletList_Click(object sender, RoutedEventArgs e) => InsertList(TextMarkerStyle.Disc);
    private void OnNumberedList_Click(object sender, RoutedEventArgs e) => InsertList(TextMarkerStyle.Decimal);

    private void InsertList(TextMarkerStyle markerStyle)
    {
        if (_activeRichTextBox == null) return;
        var sel = _activeRichTextBox.Selection;
        var list = new List { MarkerStyle = markerStyle, Foreground = Brushes.White };

        if (!sel.IsEmpty)
        {
            var lines = sel.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
                list.ListItems.Add(new ListItem(new Paragraph(new Run(line.Trim()))));
            sel.Text = "";
        }
        else
        {
            list.ListItems.Add(new ListItem(new Paragraph(new Run(""))));
        }

        var block = _activeRichTextBox.CaretPosition.Paragraph;
        if (block != null)
            _activeRichTextBox.Document.Blocks.InsertAfter(block, list);
        else
            _activeRichTextBox.Document.Blocks.Add(list);
        _activeRichTextBox.Focus();
    }

    private void OnIncreaseFontSize_Click(object sender, RoutedEventArgs e) => ChangeFontSize(2);
    private void OnDecreaseFontSize_Click(object sender, RoutedEventArgs e) => ChangeFontSize(-2);

    private void ChangeFontSize(double delta)
    {
        if (_activeRichTextBox == null) return;
        var sel = _activeRichTextBox.Selection;
        if (sel.IsEmpty) return;
        var cur = sel.GetPropertyValue(TextElement.FontSizeProperty);
        double currentSize = cur is double d ? d : 14;
        sel.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Max(8, Math.Min(48, currentSize + delta)));
        _activeRichTextBox.Focus();
    }

    // ──── Objectives ──────────────────────────────────────────────

    private void RebuildObjectivesUI()
    {
        ObjectivesPanel.Children.Clear();
        foreach (var obj in _viewModel.Objectives)
            AddObjectiveRow(obj);
    }

    private void AddObjectiveRow(EditableObjective obj)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

        var moveBtns = new StackPanel { Orientation = Orientation.Horizontal };
        var upBtn = new Button { Content = "▲", Width = 24, Height = 26, Margin = new Thickness(0, 0, 4, 0) };
        var downBtn = new Button { Content = "▼", Width = 24, Height = 26 };
        if (FindResource("CpDarkButtonStyle") is Style darkStyle)
        {
            upBtn.Style = darkStyle;
            downBtn.Style = darkStyle;
        }
        upBtn.Click += (_, _) => { _viewModel.MoveObjectiveUp(obj); RebuildObjectivesUI(); };
        downBtn.Click += (_, _) => { _viewModel.MoveObjectiveDown(obj); RebuildObjectivesUI(); };
        moveBtns.Children.Add(upBtn);
        moveBtns.Children.Add(downBtn);
        Grid.SetColumn(moveBtns, 0);

        var textBox = new TextBox { Text = obj.Text, Margin = new Thickness(4, 0, 4, 0), Height = 26 };
        if (FindResource("CpInputTextBoxStyle") is Style inputStyle)
            textBox.Style = inputStyle;
        textBox.TextChanged += (_, _) => obj.Text = textBox.Text;
        Grid.SetColumn(textBox, 1);

        var delBtn = new Button { Content = "✕", Width = 26, Height = 26 };
        if (FindResource("CpDangerButtonStyle") is Style dangerStyle)
            delBtn.Style = dangerStyle;
        delBtn.Click += (_, _) => { _viewModel.RemoveObjective(obj); RebuildObjectivesUI(); };
        Grid.SetColumn(delBtn, 2);

        grid.Children.Add(moveBtns);
        grid.Children.Add(textBox);
        grid.Children.Add(delBtn);
        ObjectivesPanel.Children.Add(grid);
    }

    private void OnAddObjective_Click(object sender, RoutedEventArgs e)
    {
        var obj = _viewModel.AddObjective();
        AddObjectiveRow(obj);
    }

    // ──── Save / Publish ──────────────────────────────────────────

    private async void OnSaveDraft_Click(object sender, RoutedEventArgs e)
    {
        SyncAllSections();

        if (_viewModel.CurrentOrder == null)
        {
            await _viewModel.CreateNewOrderAsync(TitleBox.Text, "");
            return;
        }

        await _viewModel.SaveDraftAsync(TitleBox.Text, "");
    }

    private async void OnPublish_Click(object sender, RoutedEventArgs e)
    {
        SyncAllSections();

        if (_viewModel.CurrentOrder == null)
        {
            _viewModel.StatusMessage = "Create an order first before publishing.";
            return;
        }

        await _viewModel.PublishOrderAsync(TitleBox.Text, "");
    }
}
