using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Util.Network;

namespace ticktick_WPF.Util;

public class FeishuRobotUtils
{
	private class RobotModel
	{
		public string msg_type { get; set; }

		public RobotTextModel content { get; set; }
	}

	private class RobotTextModel
	{
		public string text { get; set; }
	}

	private static string ExceptionWebHook = "https://open.feishu.cn/open-apis/bot/v2/hook/df324560-2ac9-4be8-be61-2307441362b4";

	public static void SendExceptionContent(string content)
	{
		SendContent(ExceptionWebHook, content);
	}

	private static async Task SendContent(string webHook, string content)
	{
		try
		{
			content = content.Replace("*", "");
			RobotModel value = new RobotModel
			{
				msg_type = "text",
				content = new RobotTextModel
				{
					text = content
				}
			};
			using HttpClient httpClient = HttpClientHelper.GetHttpClient();
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(webHook));
			httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
			(await httpClient.SendAsync(httpRequestMessage)).Dispose();
		}
		catch (Exception)
		{
		}
	}
}
