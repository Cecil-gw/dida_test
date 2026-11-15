using System;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Util;

[StructLayout(LayoutKind.Explicit)]
public sealed class PropVariant : IDisposable
{
	[FieldOffset(0)]
	private decimal _decimal;

	[FieldOffset(0)]
	private ushort _valueType;

	[FieldOffset(8)]
	private IntPtr _ptr2;

	[FieldOffset(8)]
	private IntPtr _ptr;

	public PropVariant(string value)
	{
		if (value == null)
		{
			throw new ArgumentException();
		}
		_valueType = 31;
		_ptr = Marshal.StringToCoTaskMemUni(value);
	}

	public PropVariant(Guid guid)
	{
		byte[] array = guid.ToByteArray();
		_valueType = 72;
		_ptr2 = Marshal.AllocCoTaskMem(array.Length);
		Marshal.Copy(array, 0, _ptr2, array.Length);
	}

	public void Dispose()
	{
		Marshal.FreeCoTaskMem(_ptr);
		_ptr = default(IntPtr);
		_ptr2 = default(IntPtr);
		GC.SuppressFinalize(this);
	}

	~PropVariant()
	{
		Dispose();
	}
}
