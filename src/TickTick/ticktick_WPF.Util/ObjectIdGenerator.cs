using System;
using System.Diagnostics;

namespace ticktick_WPF.Util;

public static class ObjectIdGenerator
{
	private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	private static readonly object _innerLock = new object();

	private static int _counter;

	private static byte[] _machineHash = GenerateHostHash();

	private static readonly byte[] _processId = GenerateProcessId();

	private static readonly object lockObject = new object();

	public static byte[] Generate()
	{
		lock (lockObject)
		{
			byte[] array = new byte[12];
			int num = 0;
			byte[] array2 = GenerateTime();
			Array.Reverse(array2);
			Array.Copy(array2, 0, array, 0, 4);
			num += 4;
			Array.Copy(_machineHash, 0, array, num, 3);
			num += 3;
			Array.Copy(_processId, 0, array, num, 2);
			num += 2;
			byte[] bytes = BitConverter.GetBytes(GenerateCounter());
			Array.Reverse(bytes);
			Array.Copy(bytes, 1, array, num, 3);
			return array;
		}
	}

	private static byte[] GenerateTime()
	{
		return BitConverter.GetBytes((uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
	}

	private static byte[] GenerateHostHash()
	{
		return Convert.FromBase64String(App.UId ?? "xxxxxxxx");
	}

	public static void RefreshMachineHash()
	{
		_machineHash = GenerateHostHash();
	}

	private static byte[] GenerateProcessId()
	{
		byte[] bytes = BitConverter.GetBytes(Process.GetCurrentProcess().Id);
		Array.Reverse(bytes);
		return bytes;
	}

	private static int GenerateCounter()
	{
		lock (_innerLock)
		{
			return _counter++;
		}
	}

	public static string GenerateString()
	{
		return BitConverter.ToString(Generate()).Replace("-", "").ToLower();
	}
}
