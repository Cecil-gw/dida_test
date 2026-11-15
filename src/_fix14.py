import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = c.replace('''        if (_instance == null) _instance = new LocalAIChatPanel();''',
'''        if (_instance == null)
        {
            try
            {
                _instance = new LocalAIChatPanel();
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\unhandled_exceptions.log",
                    DateTime.Now + " CTOR: " + ex + "\\r\\n" + ex.StackTrace + "\\r\\n");
                throw;
            }
        }''')
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('ctor log added')
