using System;
using System.Globalization;
using Newtonsoft.Json;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class ExceptionCollectModel
{
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime time { get; set; }

	public string user { get; set; }

	public string etype { get; set; }

	public string hResult { get; set; }

	public string stackTrace { get; set; }

	public string targetSite { get; set; }

	public string cultureInfo { get; set; }

	public string message { get; set; }

	public string innerMessage { get; set; }

	public string data { get; set; }

	public string innerException { get; set; }

	public string source { get; set; }

	public string kind { get; set; }

	public ExceptionCollectModel(Exception e, ExceptionType type)
	{
		time = DateTime.Now;
		user = LocalSettings.Settings.LoginUserId;
		cultureInfo = CultureInfo.CurrentCulture?.ToString() ?? "";
		etype = e.GetType()?.ToString() ?? "";
		hResult = e.HResult.ToString() ?? "";
		stackTrace = e.StackTrace;
		targetSite = e.TargetSite?.ToString() ?? "";
		message = e.Message;
		if (e.InnerException != null)
		{
			innerMessage = e.InnerException.Message;
		}
		data = e.Data?.ToString() ?? "";
		innerException = e.InnerException?.ToString() ?? "";
		source = e.Source;
		kind = type.ToString() ?? "";
		if (targetSite == "Void SyncFlush()")
		{
			message += MemoryHelper.GetCurrentSize();
		}
	}
}
