using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace ticktick_WPF;

public class WebBrowserNewWindowHandler : IDisposable
{
	[ComImport]
	[Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	public interface DWebBrowserEvents2
	{
		[DispId(250)]
		void BeforeNavigate2([In][MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object url, [In] ref object flags, [In] ref object targetFrameName, [In] ref object postData, [In] ref object headers, [In][Out][MarshalAs(UnmanagedType.VariantBool)] ref bool cancel);

		[DispId(273)]
		void NewWindow3([In][Out][MarshalAs(UnmanagedType.IDispatch)] ref object ppDisp, [In][Out][MarshalAs(UnmanagedType.VariantBool)] ref bool Cancel, [In] uint dwFlags, [In][MarshalAs(UnmanagedType.BStr)] string bstrUrlContext, [In][MarshalAs(UnmanagedType.BStr)] string bstrUrl);
	}

	private class DWebBrowserEvents2EventHandler : DWebBrowserEvents2
	{
		private readonly WebBrowserNewWindowHandler _parent;

		public DWebBrowserEvents2EventHandler(WebBrowserNewWindowHandler parent)
		{
			_parent = parent;
		}

		public void BeforeNavigate2(object pDisp, ref object url, ref object flags, ref object targetFrameName, ref object postData, ref object headers, ref bool cancel)
		{
			string text = url?.ToString();
			if (!string.IsNullOrEmpty(text) && _parent.BeforeNavigateAction != null)
			{
				cancel = cancel || _parent.BeforeNavigateAction(text, _parent.IsTopLevelBrowser(pDisp));
			}
		}

		public void NewWindow3(ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
		{
			Cancel = true;
			_parent.NewWindowAction?.Invoke(bstrUrl);
		}
	}

	private readonly System.Windows.Controls.WebBrowser _webBrowser;

	private object _activeXInstance;

	private AxHost.ConnectionPointCookie _cookie;

	public Action<string> NewWindowAction { get; }

	public Func<string, bool, bool> BeforeNavigateAction { get; }

	public static void OnNewWindowRequested(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show("Unable open link " + url + "：" + ex.Message);
		}
	}

	public WebBrowserNewWindowHandler(System.Windows.Controls.WebBrowser webBrowser, Action<string> newWindowAction, Func<string, bool, bool> beforeNavigateAction = null)
	{
		_webBrowser = webBrowser;
		NewWindowAction = newWindowAction;
		BeforeNavigateAction = beforeNavigateAction;
		Hook();
	}

	private void Hook()
	{
		try
		{
			_activeXInstance = _webBrowser.GetType().InvokeMember("ActiveXInstance", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, _webBrowser, new object[0]);
			_cookie = new AxHost.ConnectionPointCookie(_activeXInstance, new DWebBrowserEvents2EventHandler(this), typeof(DWebBrowserEvents2));
		}
		catch (Exception)
		{
		}
	}

	public void Dispose()
	{
		_cookie?.Disconnect();
		_cookie = null;
		_activeXInstance = null;
	}

	private bool IsTopLevelBrowser(object browser)
	{
		if (browser == null || _activeXInstance == null)
		{
			return false;
		}
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		try
		{
			intPtr = Marshal.GetIUnknownForObject(browser);
			intPtr2 = Marshal.GetIUnknownForObject(_activeXInstance);
			return intPtr == intPtr2;
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.Release(intPtr);
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Marshal.Release(intPtr2);
			}
		}
	}
}
