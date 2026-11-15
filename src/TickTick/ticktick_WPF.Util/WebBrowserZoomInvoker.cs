using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace ticktick_WPF.Util;

public class WebBrowserZoomInvoker
{
	private const int LOGPIXELSX = 88;

	private const int LOGPIXELSY = 90;

	private static readonly int OLECMDEXECOPT_DODEFAULT = 0;

	private static readonly int OLECMDID_OPTICAL_ZOOM = 63;

	public static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	public static readonly string EmptyHTMLFilePath = Path.Combine(AppDataPath, "Empty.html");

	public static readonly string EmptyHTML = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><style>body {overflow-y: hidden;}v\\:* {behavior:url(#default#VML);}o\\:* {behavior:url(#default#VML);}w\\:* {behavior:url(#default#VML);}.shape {behavior:url(#default#VML);}</style></head><body></body></html>";

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr ptr);

	[DllImport("user32.dll")]
	public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, long lpInitData);

	[DllImport("gdi32.dll")]
	public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

	[DllImport("user32.dll")]
	public static extern bool SetProcessDPIAware();

	private static PointF GetCurrentDIPScale()
	{
		PointF result = new PointF(1f, 1f);
		try
		{
			SetProcessDPIAware();
			IntPtr dC = GetDC(IntPtr.Zero);
			int deviceCaps = GetDeviceCaps(dC, 88);
			int deviceCaps2 = GetDeviceCaps(dC, 90);
			result.X = (float)deviceCaps / 96f;
			result.Y = (float)deviceCaps2 / 96f;
			ReleaseDC(IntPtr.Zero, dC);
			return result;
		}
		catch (Exception)
		{
			return result;
		}
	}

	private static void SetZoom(System.Windows.Controls.WebBrowser webbrowser, int zoom)
	{
		try
		{
			if (webbrowser == null)
			{
				return;
			}
			FieldInfo[] fields = webbrowser.GetType().GetFields();
			for (int i = 0; i < fields.Length; i++)
			{
				_ = fields[i];
			}
			FieldInfo field = webbrowser.GetType().GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (null != field)
			{
				object value = field.GetValue(webbrowser);
				if (value != null)
				{
					object[] args = new object[4]
					{
						OLECMDID_OPTICAL_ZOOM,
						OLECMDEXECOPT_DODEFAULT,
						zoom,
						IntPtr.Zero
					};
					value.GetType().InvokeMember("ExecWB", BindingFlags.InvokeMethod, null, value, args);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void SetZoom(System.Windows.Forms.WebBrowser webbrowser, int zoom)
	{
		try
		{
			if (webbrowser == null)
			{
				return;
			}
			FieldInfo[] fields = webbrowser.GetType().GetFields();
			for (int i = 0; i < fields.Length; i++)
			{
				_ = fields[i];
			}
			FieldInfo field = webbrowser.GetType().GetField("axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (null != field)
			{
				object value = field.GetValue(webbrowser);
				if (value != null)
				{
					object[] args = new object[4]
					{
						OLECMDID_OPTICAL_ZOOM,
						OLECMDEXECOPT_DODEFAULT,
						zoom,
						IntPtr.Zero
					};
					value.GetType().InvokeMember("ExecWB", BindingFlags.InvokeMethod, null, value, args);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void AddZoomInvoker(System.Windows.Controls.WebBrowser browser)
	{
		if (NeedZoom())
		{
			InitEmptyHTML();
			browser.LoadCompleted += browser_LoadCompleted;
		}
	}

	public static void AddZoomInvoker(System.Windows.Forms.WebBrowser browser)
	{
		if (NeedZoom())
		{
			InitEmptyHTML();
			browser.Navigated += browser_LoadCompleted;
			browser.Navigate(new Uri(EmptyHTMLFilePath));
		}
	}

	private static void browser_LoadCompleted(object sender, NavigationEventArgs e)
	{
		if (sender is System.Windows.Controls.WebBrowser webBrowser)
		{
			webBrowser.LoadCompleted -= browser_LoadCompleted;
			PointF currentDIPScale = GetCurrentDIPScale();
			if (100 != (int)(currentDIPScale.X * 100f))
			{
				SetZoom(webBrowser, (int)(currentDIPScale.X * currentDIPScale.Y * 100f));
			}
		}
	}

	private static void browser_LoadCompleted(object sender, WebBrowserNavigatedEventArgs e)
	{
		if (sender is System.Windows.Forms.WebBrowser webBrowser)
		{
			webBrowser.Navigated -= browser_LoadCompleted;
			PointF currentDIPScale = GetCurrentDIPScale();
			if (100 != (int)(currentDIPScale.X * 100f))
			{
				SetZoom(webBrowser, (int)(currentDIPScale.X * currentDIPScale.Y * 100f));
			}
		}
	}

	private static void InitEmptyHTML()
	{
		try
		{
			if (!File.Exists(EmptyHTMLFilePath))
			{
				using (FileStream fileStream = new FileStream(EmptyHTMLFilePath, FileMode.Create))
				{
					fileStream.Write(Encoding.UTF8.GetBytes(EmptyHTML), 0, Encoding.UTF8.GetByteCount(EmptyHTML));
				}
				File.SetAttributes(EmptyHTMLFilePath, FileAttributes.ReadOnly | FileAttributes.Hidden);
			}
		}
		catch (Exception)
		{
		}
	}

	private static bool NeedZoom()
	{
		if (100 != (int)(GetCurrentDIPScale().X * 100f))
		{
			return true;
		}
		return false;
	}
}
