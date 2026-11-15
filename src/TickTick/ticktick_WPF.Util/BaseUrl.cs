using System;
using System.Linq;
using ticktick_WPF.Util.Network;

namespace ticktick_WPF.Util;

public static class BaseUrl
{
	public static readonly string Domain;

	public static readonly string ApiDomain;

	public static readonly string PullDomain;

	public static string Ip;

	public static string ApiIp;

	public static string PullIp;

	public static string DomainUrl => "https://" + Domain;

	public static string ApiDomainUrl => "https://" + ApiDomain;

	public static bool IsDefault => true;

	static BaseUrl()
	{
		Domain = "dida365.com";
		ApiDomain = "api.dida365.com";
		PullDomain = "pull.dida365.com";
		if (!IsDefault)
		{
			SetIp();
		}
	}

	public static string GetDomain()
	{
		if (!IsDefault && !string.IsNullOrEmpty(Ip))
		{
			return Ip;
		}
		return Domain;
	}

	public static string GetDebugDomainUrl()
	{
		return GetApiDomain().Replace("api-", "").Replace("api.", "");
	}

	public static string GetDomainUrl()
	{
		if (!IsDefault && !string.IsNullOrEmpty(Ip))
		{
			return Ip;
		}
		return DomainUrl;
	}

	public static string GetPullDomain()
	{
		if (!IsDefault && !string.IsNullOrEmpty(PullIp))
		{
			return PullIp;
		}
		return PullDomain;
	}

	public static void SetIp()
	{
		string ip = HttpDnsHandler.GetIp(Domain);
		if (!string.IsNullOrEmpty(ip))
		{
			ApiIp = ip;
		}
		string ip2 = HttpDnsHandler.GetIp(ApiDomain);
		if (!string.IsNullOrEmpty(ip2))
		{
			Ip = ip2;
		}
		string ip3 = HttpDnsHandler.GetIp(PullDomain);
		if (!string.IsNullOrEmpty(ip3))
		{
			PullIp = ip3;
		}
	}

	public static string GetXApiDomain()
	{
		return "https://xapi." + Domain;
	}

	public static string GetApiDomain(bool getDefault = false)
	{
		if (getDefault)
		{
			return ApiDomainUrl;
		}
		if (!IsDefault && !string.IsNullOrEmpty(ApiIp))
		{
			return "https://" + ApiIp;
		}
		return ApiDomainUrl;
	}

	public static string GetAIApiDomain()
	{
		return "https://ai." + Domain;
	}

	public static string GetAIAgentApiDomain()
	{
		return ResolveAIAgentApiDomain(GetApiDomain());
	}

	public static string GetAIAgentWebUrl()
	{
		return ResolveAIAgentWebUrl(GetApiDomain());
	}

	internal static string ResolveAIAgentApiDomain(string apiDomainUrl)
	{
		return ResolveEnvironmentDomain(apiDomainUrl, "agent");
	}

	internal static string ResolveAIAgentWebUrl(string apiDomainUrl)
	{
		return ResolveEnvironmentDomain(apiDomainUrl, null) + "/agent";
	}

	private static string ResolveEnvironmentDomain(string apiDomainUrl, string serviceName)
	{
		if (string.IsNullOrWhiteSpace(apiDomainUrl))
		{
			throw new ArgumentException("API domain cannot be empty.", "apiDomainUrl");
		}
		Uri uri = new Uri(apiDomainUrl, UriKind.Absolute);
		string text = uri.Host;
		if (text.StartsWith("api-", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text.Substring("api-".Length);
			text = (string.IsNullOrEmpty(serviceName) ? text2 : (serviceName + "-" + text2));
		}
		else if (text.StartsWith("api.", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = text.Substring("api.".Length);
			text = (string.IsNullOrEmpty(serviceName) ? text3 : (serviceName + "." + text3));
		}
		else if (!string.IsNullOrEmpty(serviceName))
		{
			text = serviceName + "-" + text;
		}
		return new UriBuilder(uri)
		{
			Host = text,
			Path = string.Empty,
			Query = string.Empty,
			Fragment = string.Empty
		}.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
	}

	public static string GetExtApiDomain()
	{
		return "https://ext." + Domain;
	}

	public static string GetHostName()
	{
		string apiDomain = GetApiDomain();
		if (apiDomain.Contains("."))
		{
			string[] array = apiDomain.Split('.');
			if (array.Length >= 3)
			{
				return string.Join(".", array.Skip(array.Length - 2).ToList());
			}
		}
		return string.Empty;
	}

	public static string GetUrl(string path)
	{
		return GetApiDomain() + path;
	}

	public static string GetAIUrl(string path)
	{
		return GetAIApiDomain() + path;
	}

	public static string GetAIAgentUrl(string path)
	{
		return GetAIAgentApiDomain() + path;
	}

	public static string GetExtUrl(string path)
	{
		return GetExtApiDomain() + path;
	}

	public static string GetReleaseNoteUrl()
	{
		return "https://" + GetPullDomain() + "/windows/release_note.json";
	}
}
