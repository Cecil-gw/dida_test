using System;

namespace ticktick_WPF.UriScheme;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class UriRouteAttribute : Attribute
{
	public string Template { get; }

	public bool IsRoot { get; }

	public UriRouteAttribute(string template = null)
	{
		Template = template;
		IsRoot = string.IsNullOrEmpty(template);
	}
}
