using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TickTickUtils.Lang;

namespace ticktick_WPF.UriScheme;

public class UriRouter
{
	private readonly Dictionary<string, (Type Controller, MethodInfo Method)> _routes = new Dictionary<string, (Type, MethodInfo)>();

	private readonly Dictionary<string, Type> _controllerCache = new Dictionary<string, Type>();

	public UriRouter(Assembly assembly)
	{
		RegisterControllers(assembly);
		BuildRouteTable();
		PrintRouteTable();
	}

	private void RegisterControllers(Assembly assembly)
	{
		try
		{
			foreach (Type item in from t in assembly.GetTypes()
				where t.IsSubclassOf(typeof(UriControllerBase)) && !t.IsAbstract
				select t)
			{
				string key = (item.Name.EndsWith("Controller") ? item.Name.Substring(0, item.Name.Length - 10) : item.Name);
				_controllerCache[key] = item;
			}
		}
		catch (ReflectionTypeLoadException ex)
		{
			Exception[] loaderExceptions = ex.LoaderExceptions;
			foreach (Exception arg in loaderExceptions)
			{
				UtilLog.Error($"RegisterControllers: {arg}");
			}
		}
	}

	private string ResolveRouteTemplate(string template, string controllerName, string actionName)
	{
		return template.ToLower().Replace("[controller]", controllerName).Replace("[action]", actionName)
			.Replace("{", "")
			.Replace("}", "")
			.Trim('/');
	}

	private void BuildRouteTable()
	{
		foreach (KeyValuePair<string, Type> item in _controllerCache)
		{
			string key = item.Key;
			Type value = item.Value;
			UriRouteAttribute customAttribute = value.GetCustomAttribute<UriRouteAttribute>();
			string text = "";
			text = ((customAttribute == null) ? key : ((!customAttribute.IsRoot) ? ResolveRouteTemplate(customAttribute.Template, key, "") : ""));
			foreach (MethodInfo item2 in from m in value.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
				where !m.IsSpecialName
				select m)
			{
				UriRouteAttribute customAttribute2 = item2.GetCustomAttribute<UriRouteAttribute>();
				string name = item2.Name;
				string text2;
				if (customAttribute2 != null)
				{
					if (customAttribute2.IsRoot)
					{
						text2 = text;
					}
					else if (customAttribute2.Template.StartsWith("/"))
					{
						text2 = ResolveRouteTemplate(customAttribute2.Template.Substring(1), key, name);
					}
					else
					{
						string text3 = ResolveRouteTemplate(customAttribute2.Template, key, name);
						text2 = (string.IsNullOrEmpty(text) ? text3 : (text + "/" + text3));
					}
				}
				else
				{
					text2 = (string.IsNullOrEmpty(text) ? name : (text + "/" + name));
				}
				text2 = text2.Trim('/').ToLower();
				_routes[text2] = (value, item2);
			}
		}
	}

	private void PrintRouteTable()
	{
		Console.WriteLine("Route table found:");
		Console.WriteLine("-----------------");
		foreach (KeyValuePair<string, (Type, MethodInfo)> item in _routes.OrderBy((KeyValuePair<string, (Type Controller, MethodInfo Method)> r) => r.Key))
		{
			string text = (string.IsNullOrEmpty(item.Key) ? "(root)" : item.Key);
			Console.WriteLine(text.PadRight(25) + " -> " + item.Value.Item1.Name + "." + item.Value.Item2.Name);
		}
		Console.WriteLine("");
	}

	public object RouteRequest(string url)
	{
		try
		{
			var (key, parameters) = ParseCustomUrl(url);
			if (_routes.TryGetValue(key, out (Type, MethodInfo) value))
			{
				return (Activator.CreateInstance(value.Item1) as UriControllerBase)?.InvokeMethod(value.Item2, parameters);
			}
			return "未识别的路由";
		}
		catch (Exception ex)
		{
			return "错误: " + ex.Message;
		}
	}

	public async Task<object> RouteRequestAsync(string url)
	{
		try
		{
			var (key, parameters) = ParseCustomUrl(url);
			if (_routes.TryGetValue(key, out (Type, MethodInfo) value))
			{
				return await ((Activator.CreateInstance(value.Item1) as UriControllerBase)?.InvokeMethodAsync(value.Item2, parameters));
			}
			return "未识别的路由";
		}
		catch (Exception ex)
		{
			return "错误: " + ex.Message;
		}
	}

	private (string Path, Dictionary<string, string> Parameters) ParseCustomUrl(string url)
	{
		string text = "";
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int num = url.IndexOf("://");
		string empty = string.Empty;
		empty = ((num != -1) ? url.Substring(num + 3) : url);
		int num2 = empty.IndexOf('?');
		if (num2 >= 0)
		{
			text = empty.Substring(0, num2);
			string text2 = empty.Substring(num2 + 1);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string[] array = text2.Split('&');
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split('=');
					if (array2.Length >= 1)
					{
						string text3 = array2[0].Trim().ToLower();
						if (!string.IsNullOrWhiteSpace(text3))
						{
							string value = ((array2.Length > 1) ? Uri.UnescapeDataString(array2[1].Trim()) : "");
							dictionary[text3] = value;
						}
					}
				}
			}
		}
		else
		{
			text = empty;
		}
		text = text.Trim('/').ToLower();
		if (text.EndsWith("/"))
		{
			text = text.Substring(0, text.Length - 1);
		}
		return (Path: text, Parameters: dictionary);
	}
}
