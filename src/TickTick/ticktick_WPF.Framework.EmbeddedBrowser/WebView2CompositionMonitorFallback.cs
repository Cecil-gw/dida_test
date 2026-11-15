using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using TickTickUtils.Lang;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class WebView2CompositionMonitorFallback
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr GetAdapterMonitor(IntPtr factory, uint adapter);

	private sealed class MonitorHook
	{
		internal IntPtr Slot;

		internal IntPtr OriginalAddress;

		internal IntPtr CallbackAddress;

		internal GetAdapterMonitor Original;

		internal GetAdapterMonitor Callback;
	}

	private struct MemoryBasicInformation
	{
		internal IntPtr BaseAddress;

		internal IntPtr AllocationBase;

		internal uint AllocationProtect;

		internal UIntPtr RegionSize;

		internal uint State;

		internal uint Protect;

		internal uint Type;
	}

	private const int GetAdapterMonitorSlot = 15;

	private const uint MemoryImage = 16777216u;

	private const uint PageReadWrite = 4u;

	private static readonly object Gate = new object();

	private static readonly List<MonitorHook> Hooks = new List<MonitorHook>(2);

	private static bool _attempted;

	internal static void EnsureInstalled()
	{
		lock (Gate)
		{
			if (_attempted)
			{
				return;
			}
			_attempted = true;
			AppDomain.CurrentDomain.ProcessExit += Restore;
			AppDomain.CurrentDomain.DomainUnload += Restore;
			IntPtr factory = IntPtr.Zero;
			IntPtr ppv = IntPtr.Zero;
			try
			{
				Marshal.ThrowExceptionForHR(Direct3DCreate9Ex(32u, out factory));
				Guid iid = new Guid("81BDCBCA-64D4-426d-AE8D-AD0147F4275C");
				Marshal.ThrowExceptionForHR(Marshal.QueryInterface(factory, ref iid, out ppv));
				Install(ppv);
				Install(factory);
				UtilLog.Info("WebView2 composition monitor fallback installed: " + Hooks.Count);
			}
			catch (Exception ex)
			{
				Restore(null, EventArgs.Empty);
				UtilLog.Info("Failed to install WebView2 composition monitor fallback. " + ex);
			}
			finally
			{
				if (ppv != IntPtr.Zero)
				{
					Marshal.Release(ppv);
				}
				if (factory != IntPtr.Zero)
				{
					Marshal.Release(factory);
				}
			}
		}
	}

	private static void Install(IntPtr factory)
	{
		IntPtr intPtr = IntPtr.Add(Marshal.ReadIntPtr(factory), 15 * IntPtr.Size);
		foreach (MonitorHook hook2 in Hooks)
		{
			if (hook2.Slot == intPtr)
			{
				return;
			}
		}
		if (VirtualQuery(intPtr, out var information, (UIntPtr)(ulong)Marshal.SizeOf(typeof(MemoryBasicInformation))) == UIntPtr.Zero || information.Type != 16777216)
		{
			throw new InvalidOperationException("D3D9 monitor entry is not in a shared image data table.");
		}
		if (!GetModuleHandleEx(5u, intPtr, out var _))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		MonitorHook hook = new MonitorHook
		{
			Slot = intPtr,
			OriginalAddress = Marshal.ReadIntPtr(intPtr)
		};
		hook.Original = (GetAdapterMonitor)Marshal.GetDelegateForFunctionPointer(hook.OriginalAddress, typeof(GetAdapterMonitor));
		hook.Callback = delegate(IntPtr self, uint adapter)
		{
			IntPtr intPtr2 = hook.Original(self, adapter);
			return (!(intPtr2 != IntPtr.Zero)) ? MonitorFromWindow(IntPtr.Zero, 1u) : intPtr2;
		};
		hook.CallbackAddress = Marshal.GetFunctionPointerForDelegate(hook.Callback);
		Hooks.Add(hook);
		if (!Replace(intPtr, hook.OriginalAddress, hook.CallbackAddress))
		{
			throw new InvalidOperationException("D3D9 monitor entry changed during installation.");
		}
	}

	private unsafe static bool Replace(IntPtr slot, IntPtr expected, IntPtr replacement)
	{
		if (VirtualQuery(slot, out var information, (UIntPtr)(ulong)Marshal.SizeOf(typeof(MemoryBasicInformation))) == UIntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		uint protection = (((information.Protect & 0xF0) != 0) ? 64u : 4u);
		if (!VirtualProtect(slot, (UIntPtr)(ulong)IntPtr.Size, protection, out var oldProtection))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		try
		{
			return Interlocked.CompareExchange(ref *(IntPtr*)(void*)slot, replacement, expected) == expected;
		}
		finally
		{
			if (!VirtualProtect(slot, (UIntPtr)(ulong)IntPtr.Size, oldProtection, out var _))
			{
				UtilLog.Info("Failed to restore D3D9 monitor table protection: " + Marshal.GetLastWin32Error());
			}
		}
	}

	private static void Restore(object sender, EventArgs args)
	{
		lock (Gate)
		{
			foreach (MonitorHook hook in Hooks)
			{
				try
				{
					Replace(hook.Slot, hook.CallbackAddress, hook.OriginalAddress);
				}
				catch (Exception ex)
				{
					UtilLog.Info("Failed to restore D3D9 monitor entry. " + ex);
				}
			}
		}
	}

	[DllImport("d3d9.dll", ExactSpelling = true)]
	private static extern int Direct3DCreate9Ex(uint sdkVersion, out IntPtr factory);

	[DllImport("user32.dll", ExactSpelling = true)]
	private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint protection, out uint oldProtection);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern UIntPtr VirtualQuery(IntPtr address, out MemoryBasicInformation information, UIntPtr size);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetModuleHandleEx(uint flags, IntPtr address, out IntPtr module);
}
