using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ticktick_WPF.Util;

public class MCI
{
	private Thread thread;

	[DllImport("winmm.dll")]
	private static extern long mciSendString(string command, string returnString, int returnSize, IntPtr hwndCallback);

	private async Task PlayOnce(string file)
	{
		mciSendString($"open \"{file}\" type mpegvideo alias tickMedia", null, 0, IntPtr.Zero);
		mciSendString("play tickMedia from 0", null, 0, IntPtr.Zero);
		await Task.Delay(10000);
		mciSendString("close tickMedia", null, 0, IntPtr.Zero);
	}

	private void PlayRepeat(string file)
	{
		mciSendString($"open \"{file}\" type mpegvideo alias tickMedia", null, 0, IntPtr.Zero);
		mciSendString("play tickMedia repeat", null, 0, IntPtr.Zero);
	}

	public void Stop()
	{
		mciSendString("close tickMedia", null, 0, IntPtr.Zero);
	}

	public void Play(string file, int times)
	{
		if (times == 0)
		{
			PlayRepeat(file);
		}
		else if (times > 0)
		{
			PlayOnce(file);
		}
	}

	public void Exit()
	{
		if (thread != null)
		{
			try
			{
				thread.Abort();
			}
			catch
			{
			}
			thread = null;
		}
	}
}
