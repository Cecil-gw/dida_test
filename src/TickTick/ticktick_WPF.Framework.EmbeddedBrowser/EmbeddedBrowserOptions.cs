using System;
using System.Collections.Generic;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserOptions
{
	private readonly HashSet<string> _trustedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public string UseCase { get; set; } = "unknown";

	public string UserAgentSuffix { get; set; }

	public bool SuppressScriptErrors { get; set; } = true;

	public bool ApplyLegacyDpiZoom { get; set; } = true;

	public bool OpenNewWindowsExternally { get; set; }

	public bool EnableHostObjects { get; set; }

	public bool RestrictNavigationToTrustedOrigins { get; set; }

	public bool UseCompositionControl { get; set; }

	public bool DisableCompositionControl { get; set; }

	public bool UseTransparentBackground { get; set; }

	public IEnumerable<string> TrustedOrigins => _trustedOrigins;

	public void AddTrustedOrigin(Uri uri)
	{
		if (uri == null || !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException("Only HTTPS origins can be trusted.", "uri");
		}
		string origin = GetOrigin(uri);
		if (!string.IsNullOrEmpty(origin))
		{
			_trustedOrigins.Add(origin);
		}
	}

	public bool IsNavigationAllowed(Uri uri)
	{
		if (uri == null || !uri.IsAbsoluteUri)
		{
			return false;
		}
		if (uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (RestrictNavigationToTrustedOrigins)
		{
			if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				return _trustedOrigins.Contains(GetOrigin(uri));
			}
			return false;
		}
		return true;
	}

	internal static string GetOrigin(Uri uri)
	{
		if (uri == null || !uri.IsAbsoluteUri)
		{
			return null;
		}
		return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
	}
}
