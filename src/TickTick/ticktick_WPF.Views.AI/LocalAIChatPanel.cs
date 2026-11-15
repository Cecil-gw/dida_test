using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using Newtonsoft.Json.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.AI;

public class LocalAIChatPanel : UserControl
{
    private static LocalAIChatPanel _instance;

    private readonly StackPanel _chat = new StackPanel();
    private readonly ScrollViewer _scroll;
    private readonly TextBox _input = new TextBox
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 40,
        MaxHeight = 132,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Padding = new Thickness(10, 7, 10, 5),
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        FontSize = 13
    };
    private readonly Button _sendBtn = new Button
    {
        Content = "发送",
        Width = 68,
        Height = 30,
        IsEnabled = false,
        ToolTip = "请先在服务商中配置并启用 API"
    };
    private readonly Grid _drawerGrid = new Grid { Visibility = Visibility.Collapsed };
    private Border _innerWindow;
    private Grid _welcomeRoot;
    private ScrollViewer _historyScroll;
    private StackPanel _historyList;
    private bool _inConversation;
    private Grid _mainGrid;
    private readonly ListBox _providerList = new ListBox();
    private readonly TextBox _fName = new TextBox();
    private readonly TextBox _fBaseUrl = new TextBox();
    private readonly TextBox _fApiKey = new TextBox();
    private readonly ComboBox _fModel = new ComboBox { IsEditable = true };
    private readonly TextBox _fTemperature = new TextBox { Text = "0.3" };
    private readonly TextBox _fMaxTokens = new TextBox { Text = "2048" };
    private readonly TextBox _fNote = new TextBox();
    private AIProvider _editing;
    private bool _dirty;
    private bool _loading;
    private readonly List<(string role, string content)> _history = new List<(string, string)>();
    private string _systemPrompt = DefaultPrompt;
    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

    private const string DefaultPrompt =
        "你是 Local Tasks 桌面任务应用的内置助手,使用中文回答。\n" +
        "你可以通过输出工具调用来操作本机任务数据。若需要操作,请在回答的最前面单独输出一行 JSON:\n" +
        "{\"tool\":\"add_task\",\"title\":\"任务标题\",\"dueDate\":\"YYYY-MM-DD 或 null\",\"priority\":0,\"tags\":[\"标签\"],\"projectId\":null}\n" +
        "或 {\"tool\":\"count_tasks\"}\n" +
        "或 {\"tool\":\"list_tasks\",\"limit\":10}\n" +
        "JSON 之后可以写自然语言说明。";

    private static readonly (string name, string prompt)[] PromptPresets =
    {
        ("默认助手", DefaultPrompt),
        ("翻译官", "你是翻译助手。中文译英文,英文译中文,只输出译文。"),
        ("总结助手", "你是总结助手。把用户输入总结为不超过 5 条要点,使用中文。"),
        ("程序员", "你是资深程序员助手,用中文回答技术问题,给出简洁可运行的代码。")
    };

    private enum ButtonTone
    {
        Primary,
        Secondary,
        Ghost,
        Danger
    }

    public static void ShowIn(Border host)
    {
        AIProviderStore.Load();
        AIConversationStore.Load();
        if (_instance == null)
        {
            try
            {
                _instance = new LocalAIChatPanel();
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\\114514\\dida\\ticktick-wpf-custom\\docs\\unhandled_exceptions.log",
                    DateTime.Now + " CTOR: " + ex + "\r\n" + ex.StackTrace + "\r\n");
                throw;
            }
        }
        host.Child = _instance;
        _instance.RefreshHistory();
        if (_instance._history.Count == 0) _instance.ShowWelcome();
        _instance.RefreshSendState();
    }

    public LocalAIChatPanel()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var mainGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        _mainGrid = mainGrid;
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(218) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var historyPanel = BuildHistoryPanel();
        Grid.SetColumn(historyPanel, 0);
        mainGrid.Children.Add(historyPanel);
        var contentHost = new Grid();
        contentHost.Margin = new Thickness(0, 0, 16, 0);
        Grid.SetColumn(contentHost, 1);
        mainGrid.Children.Add(contentHost);

        _scroll = new ScrollViewer
        {
            Content = _chat,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brushes.Transparent
        };
        _scroll.Resources = new ResourceDictionary();
        _scroll.Resources[typeof(ScrollBar)] = MakeSlimScrollBarStyle();
        var innerWindow = new Border
        {
            Child = _scroll,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 14, 10, 14),
            Background = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF))
        };
        _innerWindow = innerWindow;
        _welcomeRoot = (Grid)BuildWelcome();
        contentHost.Children.Add(innerWindow);
        contentHost.Children.Add(_welcomeRoot);

        root.Children.Add(mainGrid);
        Grid.SetRow(mainGrid, 0);

        var composer = new Border
        {
            Margin = new Thickness(22, 8, 22, 4),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2)
        };
        composer.SetResourceReference(Border.BackgroundProperty, "WindowBackgroundPrimary");
        composer.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
        var composerGrid = new Grid();
        composerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        composerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        composer.Child = composerGrid;
        _input.TextChanged += (s, e) =>
        {
            _input.Height = Math.Min(Math.Max(_input.LineCount * 19 + 10, 40), 132);
        };
        _input.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                SendAsync();
            }
        };
        _input.SetResourceReference(TextBox.ForegroundProperty, "BaseColorOpacity100");
        composerGrid.Children.Add(_input);
        Grid.SetRow(_input, 0);

        var toolbar = new Grid { Margin = new Thickness(6, 0, 6, 5), Height = 32 };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var providersBtn = MakeButton("服务商", ButtonTone.Ghost, compact: true);
        providersBtn.Content = MakeIconText("M 3 5 L 13 5 M 3 11 L 13 11 M 6 3 L 6 7 M 10 9 L 10 13", "服务商", 14);
        providersBtn.ToolTip = "配置 AI 服务商";
        providersBtn.Click += (s, e) => ToggleDrawer();
        toolbar.Children.Add(providersBtn);

        var injectBtn = MakeIconButton("M 3 4 L 13 4 M 3 8 L 13 8 M 3 12 L 13 12", "注入当前待办");
        injectBtn.Click += (s, e) => { _ = InjectTodosAsync(); };
        Grid.SetColumn(injectBtn, 1);
        toolbar.Children.Add(injectBtn);

        var promptBtn = MakeIconButton("M 8 1 L 9.3 6.7 L 15 8 L 9.3 9.3 L 8 15 L 6.7 9.3 L 1 8 L 6.7 6.7 Z", "切换提示词", filled: true);
        promptBtn.Click += (s, e) => ShowPromptMenu(promptBtn);
        Grid.SetColumn(promptBtn, 2);
        toolbar.Children.Add(promptBtn);

        ApplyButtonTone(_sendBtn, ButtonTone.Primary);
        _sendBtn.Style = MakeButtonStyle(6);
        _sendBtn.Content = MakeIconText("M 3 8 L 13 8 M 9 4 L 13 8 L 9 12", "发送", 13, "TextInverseColor");
        _sendBtn.Click += async (s, e) => await SendAsync();
        Grid.SetColumn(_sendBtn, 4);
        toolbar.Children.Add(_sendBtn);
        Grid.SetRow(toolbar, 1);
        composerGrid.Children.Add(toolbar);

        root.Children.Add(composer);
        Grid.SetRow(composer, 1);
        var disclaimer = new TextBlock
        {
            Text = "内容由 AI 生成，请核查重要信息",
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 9)
        };
        disclaimer.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
        root.Children.Add(disclaimer);
        Grid.SetRow(disclaimer, 2);
        Grid.SetRow(_drawerGrid, 0);
        Grid.SetRowSpan(_drawerGrid, 3);
        Panel.SetZIndex(_drawerGrid, 50);
        root.Children.Add(_drawerGrid);

        SizeChanged += (s, e) =>
        {
            bool compact = ActualWidth < 760;
            mainGrid.ColumnDefinitions[0].Width = compact ? new GridLength(0) : new GridLength(218);
            historyPanel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            contentHost.Margin = compact
                ? new Thickness(14, 0, 14, 0)
                : new Thickness(0, 0, 16, 0);
            composer.Margin = compact
                ? new Thickness(14, 8, 14, 4)
                : new Thickness(230, 8, 16, 4);
        };
        Content = root;
        RefreshSendState();
        AppendBubble("你好，我是内置 AI 助手。配置服务商后，可以对话、查询任务或直接创建待办。", false);
    }

    private TextBlock MakeText(string text, double fontSize = 12, FontWeight? weight = null, string colorKey = "BaseColorOpacity100")
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (weight.HasValue) textBlock.FontWeight = weight.Value;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, colorKey);
        return textBlock;
    }

    private Path MakeIcon(string data, double size = 14, string colorKey = "BaseColorOpacity60", bool filled = false)
    {
        var path = new Path
        {
            Data = Geometry.Parse(data),
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            StrokeThickness = filled ? 0 : 1.4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = filled ? null : Brushes.Transparent
        };
        if (filled) path.SetResourceReference(Shape.FillProperty, colorKey);
        else path.SetResourceReference(Shape.StrokeProperty, colorKey);
        return path;
    }

    private UIElement MakeIconText(string data, string text, double iconSize = 14, string colorKey = "BaseColorOpacity80")
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var icon = MakeIcon(data, iconSize, colorKey);
        icon.Margin = new Thickness(0, 0, 6, 0);
        row.Children.Add(icon);
        var label = MakeText(text, 12, FontWeights.Normal, colorKey);
        label.Margin = new Thickness(0);
        row.Children.Add(label);
        return row;
    }

    private Button MakeButton(string text, ButtonTone tone = ButtonTone.Secondary, bool compact = false)
    {
        var button = new Button
        {
            Content = text,
            Height = compact ? 28 : 32,
            Padding = new Thickness(compact ? 10 : 14, 0, compact ? 10 : 14, 0),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(1),
            FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        ApplyButtonTone(button, tone);
        button.Style = MakeButtonStyle(6);
        return button;
    }

    private Button MakeIconButton(string data, string toolTip, bool filled = false)
    {
        var button = MakeButton("", ButtonTone.Ghost, compact: true);
        button.Width = 28;
        button.Padding = new Thickness(0);
        button.Content = MakeIcon(data, 14, "BaseColorOpacity60", filled);
        button.ToolTip = toolTip;
        System.Windows.Automation.AutomationProperties.SetName(button, toolTip);
        return button;
    }

    private void ApplyButtonTone(Button button, ButtonTone tone)
    {
        switch (tone)
        {
            case ButtonTone.Primary:
                button.SetResourceReference(Control.BackgroundProperty, "TextAccentColor");
                button.SetResourceReference(Control.ForegroundProperty, "TextInverseColor");
                button.SetResourceReference(Control.BorderBrushProperty, "TextAccentColor");
                break;
            case ButtonTone.Ghost:
                button.Background = Brushes.Transparent;
                button.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity80");
                button.BorderBrush = Brushes.Transparent;
                break;
            case ButtonTone.Danger:
                button.Background = Brushes.Transparent;
                button.SetResourceReference(Control.ForegroundProperty, "TextRedColor");
                button.BorderBrush = Brushes.Transparent;
                break;
            default:
                button.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundPrimary");
                button.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity100");
                button.SetResourceReference(Control.BorderBrushProperty, "BaseColorOpacity10");
                break;
        }
    }

    private Style MakeButtonStyle(double radius)
    {
        var style = new Style(typeof(Button));
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ButtonBorder";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        content.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
        content.SetValue(TextElement.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        border.AppendChild(content);
        template.VisualTree = border;
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.86));
        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.68));
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.38));
        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);
        template.Triggers.Add(disabled);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        return style;
    }

    private Style MakeSlimScrollBarStyle()
    {
        var st = new Style(typeof(ScrollBar));
        st.Setters.Add(new Setter(ScrollBar.WidthProperty, 8.0));
        st.Setters.Add(new Setter(UIElement.OpacityProperty, 0.35));
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.9));
        st.Triggers.Add(hover);
                // minimal scrollbar: keep default template, just slim + fade
        return st;
    }

    // ================= drawer =================
    private void ToggleDrawer()
    {
        if (_drawerGrid.Children.Count == 0)
        {
            BuildDrawer();
        }
        if (_drawerGrid.Visibility == Visibility.Visible)
        {
            CloseDrawer();
            _drawerGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            _drawerGrid.Visibility = Visibility.Visible;
        }
    }

    private void CloseDrawer()
    {
        if (_dirty && MessageBox.Show("有未保存的修改,确定丢弃?", "AI 服务商", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        {
            return;
        }
        _dirty = false;
        _drawerGrid.Visibility = Visibility.Collapsed;
    }

    private void BuildDrawer()
    {
        if (_drawerGrid.Children.Count > 0) return;
        var overlay = new Border { Background = new SolidColorBrush(Color.FromArgb(0x58, 0x00, 0x00, 0x00)) };
        overlay.PreviewMouseLeftButtonDown += (s, e) => CloseDrawer();
        var panel = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            MinWidth = 0,
            MaxWidth = 860,
            MaxHeight = 650,
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            ClipToBounds = true
        };
        panel.SetResourceReference(Border.BackgroundProperty, "PopupBackground");
        panel.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");

        var shell = new Grid();
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Margin = new Thickness(20, 16, 16, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headerText = new StackPanel();
        headerText.Children.Add(MakeText("AI 服务商", 16, FontWeights.SemiBold));
        headerText.Children.Add(MakeText("配置 OpenAI 兼容接口、模型参数与默认服务", 11, null, "BaseColorOpacity60"));
        header.Children.Add(headerText);
        var closeBtn = MakeIconButton("M 4 4 L 12 12 M 12 4 L 4 12", "关闭");
        closeBtn.Click += (s, e) => CloseDrawer();
        Grid.SetColumn(closeBtn, 1);
        header.Children.Add(closeBtn);
        shell.Children.Add(header);

        var separator = new Border { Height = 1 };
        separator.SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity10");
        shell.Children.Add(separator);
        Grid.SetRow(separator, 0);
        separator.VerticalAlignment = VerticalAlignment.Bottom;

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        shell.Children.Add(body);

        var leftRail = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(14, 14, 10, 14)
        };
        leftRail.SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity3");
        leftRail.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
        body.Children.Add(leftRail);

        var left = new Grid();
        left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        leftRail.Child = left;
        var leftBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var addBtn = MakeButton("新增", ButtonTone.Secondary, compact: true);
        addBtn.Content = MakeIconText("M 8 3 L 8 13 M 3 8 L 13 8", "新增", 13);
        addBtn.Margin = new Thickness(0, 0, 6, 0);
        addBtn.Click += (s, e) =>
        {
            if (!ConfirmDiscard()) return;
            LoadForm(new AIProvider { name = "新服务商" });
        };
        var delBtn = MakeButton("删除", ButtonTone.Danger, compact: true);
        delBtn.Click += (s, e) =>
        {
            if (_editing == null) return;
            if (MessageBox.Show("确定删除 " + _editing.name + "?", "删除", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            AIProviderStore.Remove(_editing.id);
            _editing = null; _dirty = false;
            RefreshProviderList(); ClearForm();
        };
        leftBtns.Children.Add(addBtn);
        leftBtns.Children.Add(delBtn);
        left.Children.Add(leftBtns);

        _providerList.Background = Brushes.Transparent;
        _providerList.BorderThickness = new Thickness(0);
        _providerList.Margin = new Thickness(0);
        _providerList.Padding = new Thickness(0);
        ScrollViewer.SetHorizontalScrollBarVisibility(_providerList, ScrollBarVisibility.Disabled);
        var providerTemplate = new DataTemplate();
        var providerText = new FrameworkElementFactory(typeof(TextBlock));
        providerText.SetValue(TextBlock.FontSizeProperty, 12.0);
        providerText.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        providerText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
        providerText.SetBinding(TextBlock.TextProperty, new Binding("name"));
        providerTemplate.VisualTree = providerText;
        _providerList.ItemTemplate = providerTemplate;
        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        itemStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 3)));
        var itemTemplate = new ControlTemplate(typeof(ListBoxItem));
        var itemBorder = new FrameworkElementFactory(typeof(Border));
        itemBorder.Name = "ItemBorder";
        itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        itemBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        itemBorder.SetValue(Border.PaddingProperty, new Thickness(9, 7, 9, 7));
        var itemContent = new FrameworkElementFactory(typeof(ContentPresenter));
        itemContent.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        itemContent.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        itemContent.SetValue(TextElement.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        itemBorder.AppendChild(itemContent);
        itemTemplate.VisualTree = itemBorder;
        var itemHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        itemHover.Setters.Add(new Setter(Border.BackgroundProperty, TryBrush("BaseColorOpacity5", Brushes.Transparent), "ItemBorder"));
        var itemSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        itemSelected.Setters.Add(new Setter(Border.BackgroundProperty, TryBrush("BaseColorOpacity10", Brushes.Transparent), "ItemBorder"));
        itemSelected.Setters.Add(new Setter(TextElement.ForegroundProperty, TryBrush("TextAccentColor", SystemColors.ControlTextBrush), "ItemBorder"));
        itemTemplate.Triggers.Add(itemHover);
        itemTemplate.Triggers.Add(itemSelected);
        itemStyle.Setters.Add(new Setter(Control.TemplateProperty, itemTemplate));
        _providerList.ItemContainerStyle = itemStyle;
        _providerList.SelectionChanged += (s, e) =>
        {
            if (_providerList.SelectedItem is AIProvider p)
            {
                if (!ConfirmDiscard()) { RefreshProviderList(); return; }
                LoadForm(CloneProvider(p));
            }
        };
        Grid.SetRow(_providerList, 1);
        left.Children.Add(_providerList);

        var right = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(24, 18, 24, 20)
        };
        right.Resources[typeof(ScrollBar)] = MakeSlimScrollBarStyle();
        var form = new StackPanel { MaxWidth = 560, HorizontalAlignment = HorizontalAlignment.Left };
        form.Children.Add(MakeText("连接配置", 12, FontWeights.SemiBold, "BaseColorOpacity80"));
        var formHint = MakeText("支持 Ollama、DeepSeek、SiliconFlow 及其他兼容接口。", 11, null, "BaseColorOpacity40");
        formHint.Margin = new Thickness(0, 3, 0, 14);
        form.Children.Add(formHint);
        form.Children.Add(FormRow("名称", _fName));
        form.Children.Add(FormRow("Base URL", _fBaseUrl));
        form.Children.Add(FormRow("API Key", _fApiKey));
        form.Children.Add(FormRow("模型", _fModel));
        var modelBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, -6, 0, 12) };
        var fetchBtn = MakeButton("获取模型列表", ButtonTone.Ghost, compact: true);
        fetchBtn.Click += async (s, e) => await FetchModels();
        modelBtns.Children.Add(fetchBtn);
        form.Children.Add(modelBtns);
        var half = new Grid();
        half.ColumnDefinitions.Add(new ColumnDefinition());
        half.ColumnDefinitions.Add(new ColumnDefinition());
        half.Children.Add(FormRow("Temperature", _fTemperature, 0));
        half.Children.Add(FormRow("Max Tokens", _fMaxTokens, 1));
        half.Margin = new Thickness(0, 0, 0, 2);
        form.Children.Add(half);
        form.Children.Add(FormRow("备注", _fNote));
        var presetTitle = MakeText("内置预设", 11, FontWeights.SemiBold, "BaseColorOpacity60");
        presetTitle.Margin = new Thickness(0, 4, 0, 6);
        form.Children.Add(presetTitle);
        var presetBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (var preset in AIProviderStore.Presets)
        {
            var b = MakeButton(preset.name, ButtonTone.Secondary, compact: true);
            b.Margin = new Thickness(0, 0, 6, 6);
            var url = preset.url; var md = preset.model;
            b.Click += (s, e) =>
            {
                _fBaseUrl.Text = url;
                if (string.IsNullOrEmpty(_fModel.Text)) _fModel.Text = md;
                _fName.Text = preset.name;
                _dirty = true;
            };
            presetBar.Children.Add(b);
        }
        form.Children.Add(presetBar);
        var importTitle = MakeText("从链接导入", 11, FontWeights.SemiBold, "BaseColorOpacity60");
        importTitle.Margin = new Thickness(0, 0, 0, 6);
        form.Children.Add(importTitle);
        var importBar = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        importBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        importBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var importBox = new TextBox { Margin = new Thickness(0, 0, 8, 0) };
        StyleInput(importBox);
        importBar.Children.Add(InputShell(importBox));
        var importBtn = MakeButton("导入", ButtonTone.Secondary, compact: true);
        importBtn.Click += (s, e) =>
        {
            try
            {
                var uri = new Uri(importBox.Text.Trim());
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                AIProviderStore.Load();
                var imp = AIProviderStore.Import(
                    q["name"], q["base_url"], q["api_key"], q["model"],
                    double.TryParse(q["temperature"], out var t) ? t : (double?)null,
                    int.TryParse(q["max_tokens"], out var m) ? m : (int?)null,
                    q["note"]);
                RefreshProviderList();
                MessageBox.Show("已导入服务商: " + imp.name);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败: " + ex.Message);
            }
        };
        Grid.SetColumn(importBtn, 1);
        importBar.Children.Add(importBtn);
        form.Children.Add(importBar);
        var saveBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        var saveBtn = MakeButton("保存", ButtonTone.Secondary);
        saveBtn.Margin = new Thickness(0, 0, 8, 0);
        saveBtn.Click += (s, e) => SaveForm();
        var activateBtn = MakeButton("设为激活", ButtonTone.Primary);
        activateBtn.Margin = new Thickness(0, 0, 8, 0);
        activateBtn.Click += (s, e) => SaveAndActivate();
        var testBtn = MakeButton("测试连接", ButtonTone.Ghost);
        testBtn.Click += async (s, e) => await TestConnection();
        saveBar.Children.Add(saveBtn);
        saveBar.Children.Add(activateBtn);
        saveBar.Children.Add(testBtn);
        form.Children.Add(saveBar);
        right.Content = form;
        body.Children.Add(right);
        Grid.SetColumn(right, 1);

        panel.Child = shell;
        var drawerHost = new Grid();
        drawerHost.Children.Add(overlay);
        drawerHost.Children.Add(panel);
        _drawerGrid.Children.Add(drawerHost);
        RefreshProviderList();
        ClearForm();
    }

    private bool ConfirmDiscard()
    {
        if (!_dirty) return true;
        return MessageBox.Show("有未保存的修改,确定丢弃?", "AI 服务商", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private UIElement FormRow(string label, Control input, int col = -1)
    {
        var g = new StackPanel
        {
            Margin = col == 0
                ? new Thickness(0, 0, 6, 12)
                : col == 1
                    ? new Thickness(6, 0, 0, 12)
                    : new Thickness(0, 0, 0, 12)
        };
        var lb = MakeText(label, 11, FontWeights.Normal, "BaseColorOpacity60");
        lb.Margin = new Thickness(0, 0, 0, 5);
        StyleInput(input);
        g.Children.Add(lb);
        g.Children.Add(InputShell(input));
        if (col >= 0) Grid.SetColumn(g, col);
        input.LostFocus += (s, e) => _dirty = true;
        return g;
    }

    private Border InputShell(Control input)
    {
        var shell = new Border
        {
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            MinHeight = 34,
            Padding = new Thickness(1)
        };
        shell.SetResourceReference(Border.BackgroundProperty, "WindowBackgroundPrimary");
        shell.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
        input.BorderThickness = new Thickness(0);
        input.Background = Brushes.Transparent;
        input.MinHeight = 32;
        input.VerticalContentAlignment = VerticalAlignment.Center;
        shell.Child = input;
        return shell;
    }

    private void StyleInput(Control input)
    {
        input.FontSize = 12;
        input.Padding = new Thickness(8, 5, 8, 5);
        input.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity100");
        input.SetResourceReference(Control.BorderBrushProperty, "BaseColorOpacity10");
        input.BorderThickness = new Thickness(0);
        input.Background = Brushes.Transparent;
    }

    private AIProvider CloneProvider(AIProvider p)
    {
        return new AIProvider
        {
            id = p.id, name = p.name, base_url = p.base_url, api_key = p.api_key,
            model = p.model, temperature = p.temperature, max_tokens = p.max_tokens,
            note = p.note, is_official = p.is_official
        };
    }

    private void LoadForm(AIProvider p)
    {
        _editing = p;
        _fName.Text = p.name;
        _fBaseUrl.Text = p.base_url;
        _fApiKey.Text = p.api_key;
        _fModel.Text = p.model;
        _fTemperature.Text = p.temperature.ToString("0.##");
        _fMaxTokens.Text = (p.max_tokens ?? 2048).ToString();
        _fNote.Text = p.note;
        _dirty = false;
    }

    private void ClearForm()
    {
        _editing = null;
        _fName.Text = ""; _fBaseUrl.Text = ""; _fApiKey.Text = "";
        _fModel.Text = ""; _fTemperature.Text = "0.3"; _fMaxTokens.Text = "2048"; _fNote.Text = "";
        _dirty = false;
    }

    private void RefreshProviderList()
    {
        AIProviderStore.Load();
        var items = new List<AIProvider>(AIProviderStore.Providers);
        _providerList.ItemsSource = items;
        RefreshSendState();
    }

    private void SaveForm()
    {
        if (_editing == null) return;
        ApplyForm(_editing);
        AIProviderStore.Update(_editing);
        if (AIProviderStore.Active == null) AIProviderStore.SetActive(_editing.id);
        _dirty = false;
        RefreshProviderList();
        AppendBubble("已保存服务商: " + _editing.name, false);
    }

    private void SaveAndActivate()
    {
        if (_editing == null) return;
        ApplyForm(_editing);
        AIProviderStore.Update(_editing);
        AIProviderStore.SetActive(_editing.id);
        _dirty = false;
        RefreshProviderList();
        AppendBubble("已激活服务商: " + _editing.name, false);
    }

    private void ApplyForm(AIProvider p)
    {
        p.name = _fName.Text.Trim();
        p.base_url = _fBaseUrl.Text.Trim();
        p.api_key = _fApiKey.Text.Trim();
        p.model = _fModel.Text.Trim();
        double.TryParse(_fTemperature.Text, out var t);
        p.temperature = t;
        p.max_tokens = int.TryParse(_fMaxTokens.Text, out var m) ? m : (int?)null;
        p.note = _fNote.Text;
    }

    private async Task FetchModels()
    {
        if (_fBaseUrl.Text.Trim().Length == 0) { MessageBox.Show("请先填写 Base URL"); return; }
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, _fBaseUrl.Text.Trim().TrimEnd('/') + "/models");
            var key = _fApiKey.Text.Trim();
            if (key.Length > 0) req.Headers.Add("Authorization", "Bearer " + key);
            var resp = await Http.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                MessageBox.Show("获取失败 HTTP " + (int)resp.StatusCode + "\n" + text);
                return;
            }
            var o = JObject.Parse(text);
            var ids = (o["data"] as JArray)?.Select(x => x.Value<string>("id")).Where(x => x != null).ToList();
            if (ids == null || ids.Count == 0) { MessageBox.Show("接口未返回模型列表"); return; }
            _fModel.ItemsSource = ids;
            _fModel.IsDropDownOpen = true;
            _dirty = true;
        }
        catch (UriFormatException)
        {
            MessageBox.Show("地址格式错误,请检查 Base URL(需含 http:// 或 https://)");
        }
        catch (Exception ex)
        {
            MessageBox.Show("连接失败: " + ex.Message);
        }
    }

    private async Task TestConnection()
    {
        string baseUrl = _fBaseUrl.Text.Trim();
        if (baseUrl.Length == 0) { MessageBox.Show("请先填写 Base URL"); return; }
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + "/models");
            var key = _fApiKey.Text.Trim();
            if (key.Length > 0) req.Headers.Add("Authorization", "Bearer " + key);
            var resp = await Http.SendAsync(req);
            int code = (int)resp.StatusCode;
            if (code == 401) MessageBox.Show("测试失败: API Key 无效 (401)");
            else if (code == 404) MessageBox.Show("地址可达,但未找到 /models 接口 (404)。请确认 Base URL 是否以 /v1 结尾");
            else if (resp.IsSuccessStatusCode) MessageBox.Show("连接成功 (HTTP 200)");
            else MessageBox.Show("测试返回 HTTP " + code);
        }
        catch (UriFormatException)
        {
            MessageBox.Show("地址错误: Base URL 格式不正确");
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show("连接失败: " + ex.Message);
        }
        catch (Exception ex)
        {
            MessageBox.Show("测试失败: " + ex.Message);
        }
    }

    private void RefreshSendState()
    {
        var a = AIProviderStore.Active
            ?? AIProviderStore.Providers.FirstOrDefault(p => !string.IsNullOrEmpty(p.base_url));
        bool ok = (a != null && !string.IsNullOrEmpty(a.base_url))
            || !string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.AI_ApiBaseUrl);
        _sendBtn.IsEnabled = ok;
        _sendBtn.ToolTip = ok ? null : "请先在服务商中配置并启用 API";
    }

    private (string baseUrl, string apiKey, string model, double temp, int? maxTokens) GetCallConfig()
    {
        var a = AIProviderStore.Active
            ?? AIProviderStore.Providers.FirstOrDefault(p => !string.IsNullOrEmpty(p.base_url));
        if (a != null && !string.IsNullOrEmpty(a.base_url))
        {
            return (a.base_url, a.api_key, string.IsNullOrEmpty(a.model) ? "gpt-4o-mini" : a.model, a.temperature, a.max_tokens);
        }
        var extra = LocalSettings.Settings.ExtraSettings;
        return (extra.AI_ApiBaseUrl ?? "", extra.AI_ApiKey ?? "",
            string.IsNullOrEmpty(extra.AI_Model) ? "gpt-4o-mini" : extra.AI_Model, 0.3, null);
    }

    // ================= chat =================
    private async Task SendAsync()
    {
        var text = _input.Text.TrimEnd();
        if (text.Length == 0) return;
        _input.Text = "";
        SwitchToChat();
        AppendBubble(text, true);
        _history.Add(("user", text));
        await ChatRoundAsync();
        AIConversationStore.SyncFromHistory(_history);
        RefreshHistory();
    }

    private Border AppendLoadingBubble()
    {
        var dotsText = new TextBlock { Text = "●" };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        int dots = 1;
        timer.Tick += (s, e) =>
        {
            dots = dots % 3 + 1;
            dotsText.Text = new string('●', dots);
        };
        timer.Start();
        var border = MakeBubble(dotsText, false);
        border.Tag = timer;
        _chat.Children.Add(border);
        _scroll.ScrollToEnd();
        return border;
    }

    private void RemoveBubble(Border b)
    {
        if (b.Tag is DispatcherTimer t) t.Stop();
        if (b.Parent is Panel p) p.Children.Remove(b);
    }

    private async Task ChatRoundAsync()
    {
        var cfg = GetCallConfig();
        if (cfg.baseUrl.Length == 0)
        {
            AppendBubble("尚未配置 API。请点击右上角「服务商」添加并激活一个服务商。", false);
            return;
        }
        var loading = AppendLoadingBubble();
        try
        {
            var reply = await CallApiAsync(cfg);
            if (reply == null) { RemoveBubble(loading); return; }
            var tool = TryExtractTool(reply, out var visible);
            if (tool == null)
            {
                RemoveBubble(loading);
                AppendBubble(visible, false);
                _history.Add(("assistant", visible));
                return;
            }
            // keep loading bubble, update it with progress text
            if (loading.Child is TextBlock lt) lt.Text = visible + "\n[正在执行 " + tool.Value<string>("tool") + " …]";
            string result = await ExecuteToolAsync(tool);
            _history.Add(("assistant", reply));
            _history.Add(("user", "[TOOL_RESULT] " + result + "\n请根据以上结果给出简短回答。"));
            var reply2 = await CallApiAsync(cfg);
            RemoveBubble(loading);
            if (reply2 != null)
            {
                _ = TryExtractTool(reply2, out var v2);
                AppendBubble(v2, false);
                _history.Add(("assistant", v2));
            }
        }
        catch (Exception ex)
        {
            RemoveBubble(loading);
            AppendBubble("调用失败: " + ex.Message, false);
        }
        AIConversationStore.SyncFromHistory(_history);
        RefreshHistory();
    }

    private async Task<string> CallApiAsync((string baseUrl, string apiKey, string model, double temp, int? maxTokens) cfg)
    {
        var arr = new JArray { new JObject { ["role"] = "system", ["content"] = _systemPrompt } };
        foreach (var pair in _history)
        {
            arr.Add(new JObject { ["role"] = pair.role, ["content"] = pair.content });
        }
        var body = new JObject
        {
            ["model"] = cfg.model,
            ["messages"] = arr,
            ["temperature"] = cfg.temp
        };
        if (cfg.maxTokens.HasValue) body["max_tokens"] = cfg.maxTokens.Value;
        var req = new HttpRequestMessage(HttpMethod.Post, cfg.baseUrl.Trim().TrimEnd('/') + "/chat/completions");
        req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
        if (cfg.apiKey.Length > 0) req.Headers.Add("Authorization", "Bearer " + cfg.apiKey);
        var resp = await Http.SendAsync(req);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            AppendBubble("API 返回 HTTP " + (int)resp.StatusCode + ": " + text, false);
            return null;
        }
        var o = JObject.Parse(text);
        return o["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
    }

    private JObject TryExtractTool(string reply, out string visible)
    {
        JObject tool = null;
        var m = Regex.Match(reply, @"\{[^{}]*""tool""[^{}]*\}");
        if (m.Success)
        {
            try { tool = JObject.Parse(m.Value); } catch { tool = null; }
        }
        visible = reply;
        if (tool != null)
        {
            visible = reply.Remove(m.Index, m.Length).Trim();
            if (visible.Length == 0) visible = "(正在执行操作)";
        }
        return tool;
    }

    private async Task<string> ExecuteToolAsync(JObject tool)
    {
        string name = tool.Value<string>("tool");
        switch (name)
        {
            case "add_task":
            {
                var task = new TaskModel
                {
                    title = tool.Value<string>("title") ?? "(无标题)",
                    projectId = tool.Value<string>("projectId"),
                    priority = tool.Value<int?>("priority") ?? 0
                };
                var due = tool.Value<string>("dueDate");
                if (!string.IsNullOrEmpty(due) && DateTime.TryParse(due, out var d)) task.dueDate = d;
                var tags = tool["tags"] as JArray;
                if (tags != null) task.tags = tags.Select(t => t.ToString()).ToArray();
                var saved = await TaskService.AddTask(task);
                if (saved != null) DataChangedNotifier.NotifyAddTaskToSelectView(saved);
                return "已添加任务: " + (saved?.title ?? task.title) + (saved?.dueDate != null ? ",截止 " + saved.dueDate.Value.ToString("yyyy-MM-dd") : "");
            }
            case "count_tasks":
            {
                long n = await App.Connection.ExecuteScalarAsync<long>(
                    "SELECT COUNT(*) FROM tasks WHERE deleted_at IS NULL AND status='todo'");
                return "当前未完成任务 " + n + " 个。";
            }
            case "list_tasks":
            {
                int limit = tool.Value<int?>("limit") ?? 10;
                var rows = await App.Connection.QueryAsync<TaskRow>(
                    "SELECT title, due_date FROM tasks WHERE deleted_at IS NULL AND status='todo' ORDER BY (due_date IS NULL), due_date LIMIT ?1", limit);
                if (rows.Count == 0) return "没有未完成任务。";
                return "未完成任务列表:\n" + string.Join("\n", rows.Select(r => "- " + r.title + (string.IsNullOrEmpty(r.due_date) ? "" : " (" + r.due_date.Split('T')[0] + ")")));
            }
            default:
                return "未知工具 " + name;
        }
    }

    private class TaskRow
    {
        public string title { get; set; }
        public string due_date { get; set; }
    }

    // ================= visual helpers =================
    private Border MakeBubble(UIElement content, bool isUser)
    {
        var border = new Border
        {
            Child = content,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(13, 9, 13, 9),
            Margin = new Thickness(0, 0, 0, 9),
            MaxWidth = 560,
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        };
        border.SetResourceReference(Border.BackgroundProperty, isUser ? "BaseColorOpacity5" : "WindowBackgroundPrimary");
        border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
        if (border.Child is TextBlock tb)
        {
            tb.TextWrapping = TextWrapping.Wrap;
            tb.FontSize = 13;
            tb.LineHeight = 21;
            tb.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
        }
        return border;
    }

    private void AppendBubble(string text, bool isUser)
    {
        var tb = new TextBlock { Text = text };
        var border = MakeBubble(tb, isUser);
        _chat.Children.Add(border);
        _scroll.ScrollToEnd();
    }

    private void InjectTodosClick(object sender, RoutedEventArgs e)
    {
        _ = InjectTodosAsync();
    }

    private async Task InjectTodosAsync()
    {
        var rows = await App.Connection.QueryAsync<TaskRow>(
            "SELECT title, due_date FROM tasks WHERE deleted_at IS NULL AND status='todo' ORDER BY (due_date IS NULL), due_date LIMIT 50");
        string content;
        if (rows.Count == 0) content = "[当前待办清单]\n(没有未完成任务)";
        else content = "[当前待办清单]\n" + string.Join("\n", rows.Select(r => "- " + r.title + (string.IsNullOrEmpty(r.due_date) ? "" : " (" + r.due_date.Split('T')[0] + ")")));
        AppendBubble(content, true);
        _history.Add(("user", content + "\n(以上是系统注入的待办清单,请基于它回答我接下来的问题)"));
    }

    private void ShowPromptMenu(Button anchor)
    {
        var menu = new ContextMenu();
        var itemStyle = new Style(typeof(MenuItem));
        var itemTpl = new ControlTemplate(typeof(MenuItem));
        var itemBorder = new FrameworkElementFactory(typeof(Border));
        itemBorder.Name = "bd";
        itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        itemBorder.SetValue(Border.MarginProperty, new Thickness(4, 2, 4, 2));
        itemBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        var itemContent = new FrameworkElementFactory(typeof(ContentPresenter));
        itemContent.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentProperty));
        itemContent.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 5, 8, 5));
        itemBorder.AppendChild(itemContent);
        itemTpl.VisualTree = itemBorder;
        var itemHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        itemHover.Setters.Add(new Setter(Border.BackgroundProperty, TryBrush("TextAccentColor", SystemColors.HighlightBrush)));
        itemHover.Setters.Add(new Setter(TextElement.ForegroundProperty, TryBrush("TextInverseColor", Brushes.White)));
        itemTpl.Triggers.Add(itemHover);
        itemStyle.Setters.Add(new Setter(MenuItem.TemplateProperty, itemTpl));
        menu.ItemContainerStyle = itemStyle;
        menu.SetResourceReference(Control.BackgroundProperty, "PopupBackground");
        menu.Padding = new Thickness(4);
        foreach (var preset in PromptPresets)
        {
            var mi = new MenuItem { Header = preset.name };
            var pr = preset.prompt;
            mi.Click += (s, e) =>
            {
                _systemPrompt = pr;
            };
            menu.Items.Add(mi);
        }
        menu.PlacementTarget = anchor;
        // boundary detection: flip up when not enough space below the anchor
        try
        {
            var anchorPoint = anchor.PointToScreen(new Point(0, anchor.ActualHeight));
            var wa = SystemParameters.WorkArea;
            bool spaceBelow = wa.Bottom - anchorPoint.Y > 140;
            menu.Placement = spaceBelow ? PlacementMode.Bottom : PlacementMode.Top;
        }
        catch
        {
            menu.Placement = PlacementMode.Bottom;
        }
        menu.IsOpen = true;
    }

    private Brush TryBrush(string key, Brush fallback)
    {
        return TryFindResource(key) as Brush ?? fallback;
    }

    // ================= welcome & history =================
    private UIElement BuildHistoryPanel()
    {
        var panel = new Grid { Width = 218, Margin = new Thickness(0, 0, 12, 0) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var top = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = MakeText("对话", 12, FontWeights.SemiBold, "BaseColorOpacity60");
        top.Children.Add(title);
        var newBtn = MakeIconButton("M 8 3 L 8 13 M 3 8 L 13 8", "新对话");
        newBtn.Click += (s, e) => NewSession();
        Grid.SetColumn(newBtn, 1);
        top.Children.Add(newBtn);
        panel.Children.Add(top);
        _historyScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _historyScroll.Resources[typeof(ScrollBar)] = MakeSlimScrollBarStyle();
        _historyList = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        _historyScroll.Content = _historyList;
        Grid.SetRow(_historyScroll, 1);
        panel.Children.Add(_historyScroll);
        return panel;
    }

    private void RefreshHistory()
    {
        if (_historyList == null) return;
        _historyList.Children.Clear();
        foreach (var conv in AIConversationStore.Conversations.OrderByDescending(x => x.created_at))
        {
            bool selected = conv.id == AIConversationStore.ActiveId;
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(9, 7, 9, 7),
                Margin = new Thickness(0, 0, 0, 3),
                Cursor = Cursors.Hand
            };
            border.SetResourceReference(Border.BackgroundProperty, selected ? "BaseColorOpacity10" : "BaseColorOpacity3");
            border.SetResourceReference(Border.BorderBrushProperty, selected ? "BaseColorOpacity10" : "BaseColorOpacity3");
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var icon = MakeIcon("M 3 3 L 12 3 L 12 11 L 8 11 L 5 14 L 5 11 L 3 11 Z", 13, selected ? "TextAccentColor" : "BaseColorOpacity40");
            icon.Margin = new Thickness(0, 0, 6, 0);
            row.Children.Add(icon);
            var tb = MakeText(conv.title, 12, selected ? FontWeights.Medium : FontWeights.Normal, selected ? "TextAccentColor" : "BaseColorOpacity80");
            tb.TextTrimming = TextTrimming.CharacterEllipsis;
            tb.TextWrapping = TextWrapping.NoWrap;
            Grid.SetColumn(tb, 1);
            row.Children.Add(tb);
            border.Child = row;
            var normalBackgroundKey = selected ? "BaseColorOpacity10" : "BaseColorOpacity3";
            border.MouseEnter += (s, e) => border.SetResourceReference(Border.BackgroundProperty, selected ? "BaseColorOpacity10" : "BaseColorOpacity5");
            border.MouseLeave += (s, e) => border.SetResourceReference(Border.BackgroundProperty, normalBackgroundKey);
            var convId = conv.id;
            border.MouseLeftButtonDown += (s, e) =>
            {
                LoadConversation(convId);
            };
            _historyList.Children.Add(border);
        }
    }

    private void LoadConversation(string id)
    {
        var conv = AIConversationStore.Conversations.FirstOrDefault(x => x.id == id);
        if (conv == null) return;
        AIConversationStore.ActiveId = id;
        _history.Clear();
        foreach (var m in conv.messages) _history.Add((m.role, m.content));
        _chat.Children.Clear();
        foreach (var m in conv.messages.Where(x => x.role != "system"))
        {
            AppendBubble(m.content, m.role == "user");
        }
        SwitchToChat();
        RefreshHistory();
    }

    private void NewSession()
    {
        _history.Clear();
        _chat.Children.Clear();
        AIConversationStore.ActiveId = null;
        ShowWelcome();
        RefreshHistory();
        RefreshSendState();
    }

    private void ShowWelcome()
    {
        _inConversation = false;
        if (_welcomeRoot != null) _welcomeRoot.Visibility = Visibility.Visible;
        if (_scroll != null) _scroll.Visibility = Visibility.Collapsed;
        if (_innerWindow != null)
        {
            _innerWindow.Background = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));
            _innerWindow.BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
            _innerWindow.BorderThickness = new Thickness(1);
        }
        RefreshSendState();
    }

    private void SwitchToChat()
    {
        _inConversation = true;
        if (_welcomeRoot != null) _welcomeRoot.Visibility = Visibility.Collapsed;
        if (_scroll != null) _scroll.Visibility = Visibility.Visible;
        if (_innerWindow != null)
        {
            _innerWindow.Background = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));
            _innerWindow.BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
            _innerWindow.BorderThickness = new Thickness(1);
        }
        RefreshSendState();
    }

    private UIElement BuildWelcome()
    {
        var root = new Grid();
        var content = new StackPanel
        {
            MaxWidth = 620,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(26, 24, 26, 20)
        };
        content.Children.Add(MakeText("今天想做什么？", 20, FontWeights.SemiBold, "BaseColorOpacity100"));
        var hint = MakeText("我可以帮你梳理任务、生成计划，也可以在确认后创建待办。", 12, null, "BaseColorOpacity60");
        hint.Margin = new Thickness(0, 6, 0, 18);
        content.Children.Add(hint);

        var optionsHost = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };
        optionsHost.SetResourceReference(Border.BackgroundProperty, "WindowBackgroundPrimary");
        optionsHost.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
        var options = new StackPanel();
        var prompts = new (string title, string description, string icon, string prompt)[]
        {
            ("查看今天的任务", "列出今天未完成的事项", "M 3 4 L 13 4 M 3 8 L 13 8 M 3 12 L 13 12", "今天有哪些任务"),
            ("总结今天", "整理今天完成的进展", "M 4 3 L 12 3 L 12 13 L 4 13 Z M 6 6 L 10 6 M 6 9 L 10 9", "总结一下今天"),
            ("规划今日安排", "根据当前待办生成计划", "M 3 5 L 13 5 M 5 3 L 5 7 M 11 9 L 11 13 M 3 11 L 13 11", "规划今日安排")
        };
        for (int i = 0; i < prompts.Length; i++)
        {
            var item = prompts[i];
            var row = MakeButton("", ButtonTone.Ghost);
            row.Height = 66;
            row.Padding = new Thickness(14, 0, 12, 0);
            row.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            row.BorderThickness = new Thickness(0);
            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var icon = MakeIcon(item.icon, 15, "BaseColorOpacity60");
            icon.HorizontalAlignment = HorizontalAlignment.Left;
            rowGrid.Children.Add(icon);
            var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var title = MakeText(item.title, 12, FontWeights.Medium, "BaseColorOpacity100");
            title.LineHeight = 17;
            title.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            labels.Children.Add(title);
            var description = MakeText(item.description, 11, null, "BaseColorOpacity40");
            description.LineHeight = 16;
            description.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            description.Margin = new Thickness(0, 1, 0, 0);
            labels.Children.Add(description);
            Grid.SetColumn(labels, 1);
            rowGrid.Children.Add(labels);
            var arrow = MakeIcon("M 6 3 L 11 8 L 6 13", 13, "BaseColorOpacity40");
            Grid.SetColumn(arrow, 2);
            rowGrid.Children.Add(arrow);
            row.Content = rowGrid;
            var prompt = item.prompt;
            row.Click += (s, e) => SendText(prompt);
            options.Children.Add(row);
            if (i < prompts.Length - 1)
            {
                var divider = new Border { Height = 1, Margin = new Thickness(14, 0, 12, 0) };
                divider.SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity10");
                options.Children.Add(divider);
            }
        }
        optionsHost.Child = options;
        content.Children.Add(optionsHost);
        root.Children.Add(content);
        return root;
    }

    private async void SendText(string text)
    {
        if (_loading) return;
        SwitchToChat();
        AppendBubble(text, true);
        _history.Add(("user", text));
        await ChatRoundAsync();
        AIConversationStore.SyncFromHistory(_history);
        RefreshHistory();
    }
}


