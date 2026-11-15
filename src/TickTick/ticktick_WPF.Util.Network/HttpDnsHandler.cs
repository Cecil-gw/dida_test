using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util.Network;

public static class HttpDnsHandler
{
	public static string GetIp(string host)
	{
		try
		{
			string uriString = "https://203.107.1.1/186062/d?host=" + host;
			using HttpClient httpClient = new HttpClient();
			HostIpsModel hostIpsModel = JsonConvert.DeserializeObject<HostIpsModel>(httpClient.GetAsync(new Uri(uriString)).Result.Content.ReadAsStringAsync().Result);
			if (hostIpsModel != null && hostIpsModel.ips?.Length > 0)
			{
				return hostIpsModel.ips.LastOrDefault();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
		return string.Empty;
	}
}
