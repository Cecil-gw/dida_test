import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# 1. providers button: capsule style + >=64px right safety gap
old = '''        var providersBtn = MakeToolButton("服务商");
        providersBtn.Click += (s, e) => ToggleDrawer();
        DockPanel.SetDock(providersBtn, Dock.Right);
        top.Children.Add(providersBtn);'''
new = '''        var providersBtn = MakeToolButton("服务商");
        providersBtn.Click += (s, e) => ToggleDrawer();
        providersBtn.Margin = new Thickness(3, 0, 68, 0);
        DockPanel.SetDock(providersBtn, Dock.Right);
        top.Children.Add(providersBtn);'''
assert old in c
c = c.replace(old, new, 1)

# 2. MakeToolButton: native-consistent pill (rounded 14, theme brushes, hover states)
old_btn = '''    private static Button MakeToolButton(string text)
    {
        return new Button { Content = text, Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(3, 0, 0, 0), Cursor = Cursors.Hand };
    }'''
new_btn = '''    private static Button MakeToolButton(string text)
    {
        var b = new Button
        {
            Content = text,
            Padding = new Thickness(14, 4, 14, 4),
            Margin = new Thickness(3, 0, 0, 0),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(0),
            Background = (Brush)Application.Current.TryFindResource("PopupBackground") ?? new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
            Foreground = (Brush)Application.Current.TryFindResource("TextPrimary") ?? SystemColors.ControlTextBrush
        };
        var st = new Style(typeof(Button));
        var tpl = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "bd";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
        border.SetBinding(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        border.SetBinding(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        border.SetBinding(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentProperty));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        tpl.VisualTree = border;
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(OpacityProperty, 0.85));
        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(OpacityProperty, 0.7));
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(OpacityProperty, 0.35));
        tpl.Triggers.Add(hover);
        tpl.Triggers.Add(pressed);
        tpl.Triggers.Add(disabled);
        st.Setters.Add(new Setter(Button.TemplateProperty, tpl));
        b.Style = st;
        return b;
    }'''
assert old_btn in c
c = c.replace(old_btn, new_btn, 1)

# 3. prompt menu: flip up when not enough space below; rounded card menu; items hover rounded
old_menu = '''    private void ShowPromptMenu(Button anchor)
    {
        var menu = new ContextMenu();
        foreach (var preset in PromptPresets)
        {
            var mi = new MenuItem { Header = preset.name };
            var pr = preset.prompt;
            mi.Click += (s, e) =>
            {
                _systemPrompt = pr;
                _statusText.Text = "提示词: " + preset.name;
            };
            menu.Items.Add(mi);
        }
        menu.PlacementTarget = anchor;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }'''
new_menu = '''    private void ShowPromptMenu(Button anchor)
    {
        var menu = new ContextMenu();
        var itemStyle = new Style(typeof(MenuItem));
        var itemTpl = new ControlTemplate(typeof(MenuItem));
        var itemBorder = new FrameworkElementFactory(typeof(Border));
        itemBorder.Name = "bd";
        itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        itemBorder.SetValue(Border.MarginProperty, new Thickness(4, 2, 4, 2));
        itemBorder.SetBinding(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        var itemContent = new FrameworkElementFactory(typeof(ContentPresenter));
        itemContent.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentProperty));
        itemContent.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 5, 8, 5));
        itemBorder.AppendChild(itemContent);
        itemTpl.VisualTree = itemBorder;
        var itemHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        itemHover.Setters.Add(new Setter(Border.BackgroundProperty, (Brush)Application.Current.TryFindResource("AccentPrimary") ?? new SolidColorBrush(Color.FromRgb(0x4F, 0x7C, 0xFF))));
        itemHover.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White));
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
                _statusText.Text = "提示词: " + preset.name;
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
    }'''
assert old_menu in c
c = c.replace(old_menu, new_menu, 1)

with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('UI polish patched')
