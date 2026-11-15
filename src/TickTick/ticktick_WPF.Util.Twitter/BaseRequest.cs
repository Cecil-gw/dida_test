using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ticktick_WPF.Util.Twitter;

public class BaseRequest
{
	private const string SIGNATURE_METHOD = "HMAC-SHA1";

	protected string m_UrlPrefix = string.Empty;

	private OAuthInfo m_OAuthInfo;

	public static string oauth_timestamp = "";

	public static string oauth_nonce = "";

	public static string request_token = "";

	public static string oauth_token_secret = "";

	public static string signature = "";

	public OAuthInfo OAuthInfo => m_OAuthInfo;

	public BaseRequest(string urlPrefix, OAuthInfo oAuthInfo)
	{
		if (string.IsNullOrEmpty(urlPrefix))
		{
			throw new ArgumentNullException("urlPrefix");
		}
		if (!urlPrefix.EndsWith("/"))
		{
			urlPrefix += "/";
		}
		m_UrlPrefix = urlPrefix;
		m_OAuthInfo = oAuthInfo;
	}

	private string GetSignatureForGetRequest(string url, Dictionary<string, string> parameters)
	{
		StringBuilder stringBuilder = new StringBuilder().Append("GET").Append("&").Append(url.EncodeRFC3986())
			.Append("&")
			.Append((from x in parameters
				orderby x.Key
				select $"{x.Key}={x.Value}").Join("&").EncodeRFC3986());
		return GetSignature(stringBuilder.ToString());
	}

	public virtual async Task<string> GetTwitterRequestTokenAsync(string twitterCallbackUrl)
	{
		string url = "https://api.twitter.com/oauth/request_token";
		oauth_nonce = GetNonce();
		oauth_timestamp = GetTimeStamp();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("oauth_callback", Uri.EscapeDataString(twitterCallbackUrl));
		dictionary.Add("oauth_consumer_key", m_OAuthInfo.ConsumerKey);
		dictionary.Add("oauth_nonce", oauth_nonce);
		dictionary.Add("oauth_signature_method", "HMAC-SHA1");
		dictionary.Add("oauth_timestamp", oauth_timestamp);
		dictionary.Add("oauth_version", "1.0");
		signature = GetSignatureForGetRequest(url, dictionary);
		string requestUrl = GetRequestUrl(url, dictionary);
		requestUrl = requestUrl + "&oauth_signature=" + Uri.EscapeDataString(signature);
		HttpClient httpClient = new HttpClient();
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		_ = string.Empty;
		string text;
		try
		{
			text = await httpClient.GetStringAsync(new Uri(requestUrl));
		}
		catch (Exception)
		{
			return string.Empty;
		}
		string[] array = text.Split('&');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split('=');
			string text2 = array2[0];
			if (!(text2 == "oauth_token"))
			{
				if (text2 == "oauth_token_secret")
				{
					oauth_token_secret = array2[1];
				}
			}
			else
			{
				request_token = array2[1];
			}
		}
		return request_token;
	}

	private string GetNonce()
	{
		return new Random().Next(1000000000).ToString();
	}

	private string GetTimeStamp()
	{
		return Math.Round((DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
	}

	private string GetRequestUrl(string url, Dictionary<string, string> parameters)
	{
		return $"{url}?{GetCustomParametersString(parameters)}";
	}

	private string GetCustomParametersString(Dictionary<string, string> parameters)
	{
		return parameters.Select((KeyValuePair<string, string> x) => $"{x.Key}={x.Value}").Join("&");
	}

	private string GetSignature(string sigBaseString)
	{
		string s = Uri.EscapeDataString(m_OAuthInfo.ConsumerSecret) + "&";
		using HMACSHA1 hMACSHA = new HMACSHA1(Encoding.UTF8.GetBytes(s));
		return Convert.ToBase64String(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes(sigBaseString)));
	}
}
