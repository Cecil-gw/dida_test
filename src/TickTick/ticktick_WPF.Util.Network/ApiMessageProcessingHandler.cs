using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Network;

internal class ApiMessageProcessingHandler : MessageProcessingHandler
{
	private readonly List<(string, string)> _extraHeaders;

	public ApiMessageProcessingHandler()
	{
	}

	public ApiMessageProcessingHandler(List<(string, string)> extraHeaders)
	{
		_extraHeaders = extraHeaders;
	}

	protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		HttpRequestHeaders headers = request.Headers;
		headers.Add("User-Agent", "TickTick/W-" + Utils.GetVersion());
		headers.Add("x-device", Utils.GetDeviceInfo());
		headers.Add("x-timestamp", Utils.GetXTimeStamp());
		headers.Add("traceid", DateTime.UtcNow.Ticks.ToString() ?? "");
		headers.Add("hl", Utils.GetLanguage());
		headers.Add("x-tz", TimeZoneData.LocalTimeZoneModel.TimeZoneName);
		if (_extraHeaders != null)
		{
			foreach (var extraHeader in _extraHeaders)
			{
				headers.Add(extraHeader.Item1, extraHeader.Item2);
			}
		}
		string loginUserAuth = LocalSettings.Settings.LoginUserAuth;
		if (!string.IsNullOrEmpty(loginUserAuth))
		{
			headers.Add("Authorization", "OAuth " + loginUserAuth);
		}
		UtilHttp.FixContent(request);
		return request;
	}

	protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		return response;
	}
}
