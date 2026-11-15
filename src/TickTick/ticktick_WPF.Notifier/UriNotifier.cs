using System;
using System.Collections.Generic;

namespace ticktick_WPF.Notifier;

internal class UriNotifier
{
	public static event EventHandler<UriModel> Uri;

	public static void NotifyUri(string path, string param)
	{
		UriModel e = new UriModel
		{
			Path = path,
			Param = param,
			Parmas = new Dictionary<string, string>()
		};
		try
		{
			string[] array = param.Split('&');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				string key = array2[0];
				string value = array2[1];
				e.Parmas.Add(key, value);
			}
		}
		catch (Exception)
		{
		}
		UriNotifier.Uri?.Invoke(null, e);
	}
}
