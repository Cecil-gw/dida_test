using System.Net;

namespace ticktick_WPF.Util.Network;

public class ProxyModel
{
	internal readonly string key;

	internal readonly IWebProxy proxy;

	public override bool Equals(object obj)
	{
		if (obj is ProxyModel proxyModel)
		{
			return key.Equals(proxyModel.key);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return key.GetHashCode();
	}

	private ProxyModel(string key, IWebProxy proxy)
	{
		this.key = key;
		this.proxy = proxy;
	}

	internal bool IsSame(ProxyModel other)
	{
		if (other != null)
		{
			return key.Equals(other.key);
		}
		return false;
	}

	public override string ToString()
	{
		return "ProxyModel=" + key;
	}

	public static ProxyModel Create(int type, string addressStr, string portStr, string user, string pwd, string domain)
	{
		string text = "type=" + type;
		IWebProxy webProxy = null;
		switch (type)
		{
		case 2:
			webProxy = WebRequest.GetSystemWebProxy();
			break;
		case 1:
		{
			text = text + ", address=" + addressStr + ":" + portStr;
			if (!HttpClientHelper.TryGetEndPoint(addressStr, portStr, out var endPoint))
			{
				break;
			}
			webProxy = new WebProxy(endPoint);
			if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pwd))
			{
				if (domain == null)
				{
					domain = string.Empty;
				}
				text = text + ", user=" + user + ", pwd=" + pwd.GetHashCode() + ", domain=" + domain;
				webProxy.Credentials = new NetworkCredential(user, Utils.Base64Decode(pwd), domain);
			}
			break;
		}
		}
		return new ProxyModel(text, webProxy);
	}
}
