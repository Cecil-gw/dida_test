using System;

namespace ticktick_WPF.UriScheme;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public class UriParamAttribute : Attribute
{
	public string Name { get; }

	public UriParamAttribute(string name = null)
	{
		Name = name;
	}
}
