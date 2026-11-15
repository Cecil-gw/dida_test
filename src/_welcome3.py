import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = c.replace('''        _input.Text = "";
        AppendBubble(text, true);
        _history.Add(("user", text));
        await ChatRoundAsync();
    }''',
'''        _input.Text = "";
        SwitchToChat();
        AppendBubble(text, true);
        _history.Add(("user", text));
        await ChatRoundAsync();
        AIConversationStore.SyncFromHistory(_history);
        RefreshHistory();
    }''')
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('SendAsync synced')
