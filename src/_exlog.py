import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF\App.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
LOG = 'System.IO.File.AppendAllText(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\unhandled_exceptions.log", DateTime.Now + " " + e.Exception + "\\r\\n");'
c = c.replace('\t\tUtilLog.Warn("TaskUnhandledException() " + e.Exception);',
              '\t\tUtilLog.Warn("TaskUnhandledException() " + e.Exception);\n\t\t' + LOG.replace('e.Exception','e.Exception'))
c = c.replace('\t\t\tUtilLog.Error("DispatcherUnhandledException() " + e.Exception);',
              '\t\t\tUtilLog.Error("DispatcherUnhandledException() " + e.Exception);\n\t\t\t' + LOG)
c = c.replace('\t\t\tUtilLog.Error("CurrentDomain_UnhandledException() " + ex);',
              '\t\t\tUtilLog.Error("CurrentDomain_UnhandledException() " + ex);\n\t\t\t' + LOG.replace('e.Exception','ex'))
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('exception logging added')
