using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Util.TouchPad;

public class TouchPadHelper
{
	internal static class TouchpadHelper
	{
		private struct RAWINPUTDEVICELIST
		{
			public IntPtr hDevice;

			public uint dwType;
		}

		private struct RAWINPUTDEVICE
		{
			public ushort usUsagePage;

			public ushort usUsage;

			public uint dwFlags;

			public IntPtr hwndTarget;
		}

		private struct RAWINPUT
		{
			public RAWINPUTHEADER Header;

			public RAWHID Hid;
		}

		private struct RAWINPUTHEADER
		{
			public uint dwType;

			public uint dwSize;

			public IntPtr hDevice;

			public IntPtr wParam;
		}

		private struct RAWHID
		{
			public uint dwSizeHid;

			public uint dwCount;

			public IntPtr bRawData;
		}

		private struct RID_DEVICE_INFO
		{
			public uint cbSize;

			public uint dwType;

			public RID_DEVICE_INFO_HID hid;
		}

		private struct RID_DEVICE_INFO_HID
		{
			public uint dwVendorId;

			public uint dwProductId;

			public uint dwVersionNumber;

			public ushort usUsagePage;

			public ushort usUsage;
		}

		private struct HIDP_CAPS
		{
			public ushort Usage;

			public ushort UsagePage;

			public ushort InputReportByteLength;

			public ushort OutputReportByteLength;

			public ushort FeatureReportByteLength;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
			public ushort[] Reserved;

			public ushort NumberLinkCollectionNodes;

			public ushort NumberInputButtonCaps;

			public ushort NumberInputValueCaps;

			public ushort NumberInputDataIndices;

			public ushort NumberOutputButtonCaps;

			public ushort NumberOutputValueCaps;

			public ushort NumberOutputDataIndices;

			public ushort NumberFeatureButtonCaps;

			public ushort NumberFeatureValueCaps;

			public ushort NumberFeatureDataIndices;
		}

		private enum HIDP_REPORT_TYPE
		{
			HidP_Input,
			HidP_Output,
			HidP_Feature
		}

		private struct HIDP_VALUE_CAPS
		{
			public ushort UsagePage;

			public byte ReportID;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsAlias;

			public ushort BitField;

			public ushort LinkCollection;

			public ushort LinkUsage;

			public ushort LinkUsagePage;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsRange;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsStringRange;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsDesignatorRange;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsAbsolute;

			[MarshalAs(UnmanagedType.U1)]
			public bool HasNull;

			public byte Reserved;

			public ushort BitSize;

			public ushort ReportCount;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public ushort[] Reserved2;

			public uint UnitsExp;

			public uint Units;

			public int LogicalMin;

			public int LogicalMax;

			public int PhysicalMin;

			public int PhysicalMax;

			public ushort UsageMin;

			public ushort UsageMax;

			public ushort StringMin;

			public ushort StringMax;

			public ushort DesignatorMin;

			public ushort DesignatorMax;

			public ushort DataIndexMin;

			public ushort DataIndexMax;

			public ushort Usage => UsageMin;

			public ushort StringIndex => StringMin;

			public ushort DesignatorIndex => DesignatorMin;

			public ushort DataIndex => DataIndexMin;
		}

		private const uint RIM_TYPEMOUSE = 0u;

		private const uint RIM_TYPEKEYBOARD = 1u;

		private const uint RIM_TYPEHID = 2u;

		private const uint RIDEV_INPUTSINK = 256u;

		private const uint RID_INPUT = 268435459u;

		private const uint RIDI_PREPARSEDDATA = 536870917u;

		private const uint RIDI_DEVICEINFO = 536870923u;

		private const uint HIDP_STATUS_SUCCESS = 1114112u;

		public const int WM_INPUT = 255;

		public const int RIM_INPUT = 0;

		public const int RIM_INPUTSINK = 1;

		[DllImport("User32", SetLastError = true)]
		private static extern uint GetRawInputDeviceList([Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, ref RID_DEVICE_INFO pData, ref uint pcbSize);

		[DllImport("Hid.dll", SetLastError = true)]
		private static extern uint HidP_GetCaps(IntPtr PreparsedData, out HIDP_CAPS Capabilities);

		[DllImport("Hid.dll", CharSet = CharSet.Auto)]
		private static extern uint HidP_GetValueCaps(HIDP_REPORT_TYPE ReportType, [Out] HIDP_VALUE_CAPS[] ValueCaps, ref ushort ValueCapsLength, IntPtr PreparsedData);

		[DllImport("Hid.dll", CharSet = CharSet.Auto)]
		private static extern uint HidP_GetUsageValue(HIDP_REPORT_TYPE ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out uint UsageValue, IntPtr PreparsedData, IntPtr Report, uint ReportLength);

		public static bool Exists()
		{
			uint puiNumDevices = 0u;
			uint cbSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();
			if (GetRawInputDeviceList(null, ref puiNumDevices, cbSize) != 0)
			{
				return false;
			}
			RAWINPUTDEVICELIST[] array = new RAWINPUTDEVICELIST[puiNumDevices];
			if (GetRawInputDeviceList(array, ref puiNumDevices, cbSize) != puiNumDevices)
			{
				return false;
			}
			foreach (RAWINPUTDEVICELIST item in array.Where((RAWINPUTDEVICELIST x) => x.dwType == 2))
			{
				uint pcbSize = 0u;
				if (GetRawInputDeviceInfo(item.hDevice, 536870923u, IntPtr.Zero, ref pcbSize) == 0)
				{
					RID_DEVICE_INFO pData = new RID_DEVICE_INFO
					{
						cbSize = pcbSize
					};
					if (GetRawInputDeviceInfo(item.hDevice, 536870923u, ref pData, ref pcbSize) != uint.MaxValue && pData.hid.usUsagePage == 13 && pData.hid.usUsage == 5)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool RegisterInput(IntPtr windowHandle)
		{
			RAWINPUTDEVICE rAWINPUTDEVICE = new RAWINPUTDEVICE
			{
				usUsagePage = 13,
				usUsage = 5,
				dwFlags = 0u,
				hwndTarget = windowHandle
			};
			return RegisterRawInputDevices(new RAWINPUTDEVICE[1] { rAWINPUTDEVICE }, 1u, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
		}

		public static TouchpadContact[] ParseInput(IntPtr lParam)
		{
			uint pcbSize = 0u;
			uint cbSizeHeader = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
			if (GetRawInputData(lParam, 268435459u, IntPtr.Zero, ref pcbSize, cbSizeHeader) != 0)
			{
				return null;
			}
			IntPtr intPtr = IntPtr.Zero;
			RAWINPUT rAWINPUT;
			byte[] array2;
			try
			{
				intPtr = Marshal.AllocHGlobal((int)pcbSize);
				if (GetRawInputData(lParam, 268435459u, intPtr, ref pcbSize, cbSizeHeader) != pcbSize)
				{
					return null;
				}
				rAWINPUT = Marshal.PtrToStructure<RAWINPUT>(intPtr);
				byte[] array = new byte[pcbSize];
				Marshal.Copy(intPtr, array, 0, array.Length);
				array2 = new byte[rAWINPUT.Hid.dwSizeHid * rAWINPUT.Hid.dwCount];
				int num = (int)pcbSize - array2.Length;
				if (num >= 0)
				{
					Buffer.BlockCopy(array, num, array2, 0, array2.Length);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
			IntPtr intPtr2 = Marshal.AllocHGlobal(array2.Length);
			Marshal.Copy(array2, 0, intPtr2, array2.Length);
			IntPtr intPtr3 = IntPtr.Zero;
			try
			{
				uint pcbSize2 = 0u;
				if (GetRawInputDeviceInfo(rAWINPUT.Header.hDevice, 536870917u, IntPtr.Zero, ref pcbSize2) != 0)
				{
					return null;
				}
				intPtr3 = Marshal.AllocHGlobal((int)pcbSize2);
				if (GetRawInputDeviceInfo(rAWINPUT.Header.hDevice, 536870917u, intPtr3, ref pcbSize2) != pcbSize2)
				{
					return null;
				}
				if (HidP_GetCaps(intPtr3, out var Capabilities) != 1114112)
				{
					return null;
				}
				ushort ValueCapsLength = Capabilities.NumberInputValueCaps;
				HIDP_VALUE_CAPS[] array3 = new HIDP_VALUE_CAPS[ValueCapsLength];
				if (HidP_GetValueCaps(HIDP_REPORT_TYPE.HidP_Input, array3, ref ValueCapsLength, intPtr3) != 1114112)
				{
					return null;
				}
				uint num2 = 0u;
				TouchpadContactCreator touchpadContactCreator = new TouchpadContactCreator();
				List<TouchpadContact> list = new List<TouchpadContact>();
				foreach (HIDP_VALUE_CAPS item in array3.OrderBy((HIDP_VALUE_CAPS x) => x.LinkCollection))
				{
					if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, item.UsagePage, item.LinkCollection, item.Usage, out var UsageValue, intPtr3, intPtr2, (uint)array2.Length) != 1114112)
					{
						continue;
					}
					if (item.LinkCollection == 0)
					{
						if ((item.UsagePage != 13 || item.Usage != 86) && item.UsagePage == 13 && item.Usage == 84)
						{
							num2 = UsageValue;
						}
					}
					else if (item.UsagePage == 13 && item.Usage == 81)
					{
						touchpadContactCreator.ContactId = (int)UsageValue;
					}
					else if (item.UsagePage == 1 && item.Usage == 48)
					{
						touchpadContactCreator.X = (int)UsageValue;
					}
					else if (item.UsagePage == 1 && item.Usage == 49)
					{
						touchpadContactCreator.Y = (int)UsageValue;
					}
					if (touchpadContactCreator.TryCreate(out var contact))
					{
						list.Add(contact);
						if (list.Count >= num2)
						{
							break;
						}
						touchpadContactCreator.Clear();
					}
				}
				return list.ToArray();
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr2);
				Marshal.FreeHGlobal(intPtr3);
			}
		}
	}
}
