using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ticktick_WPF.UriScheme;

public abstract class UriControllerBase
{
	public object InvokeMethod(MethodInfo method, Dictionary<string, string> parameters)
	{
		ParameterInfo[] parameters2 = method.GetParameters();
		object[] array = new object[parameters2.Length];
		for (int i = 0; i < parameters2.Length; i++)
		{
			ParameterInfo parameterInfo = parameters2[i];
			Type parameterType = parameterInfo.ParameterType;
			if (!IsSimpleType(parameterType))
			{
				array[i] = BindComplexType(parameterType, parameters);
				continue;
			}
			string text = (parameterInfo.GetCustomAttribute<UriParamAttribute>()?.Name ?? parameterInfo.Name)?.ToLower();
			if (text != null && parameters.TryGetValue(text, out var value) && !string.IsNullOrEmpty(value))
			{
				try
				{
					array[i] = ConvertValue(value, parameterType);
				}
				catch (Exception)
				{
					array[i] = GetDefaultValue(parameterType);
				}
			}
			else if (parameterInfo.HasDefaultValue && parameterInfo.DefaultValue != DBNull.Value)
			{
				array[i] = parameterInfo.DefaultValue;
			}
			else
			{
				array[i] = GetDefaultValue(parameterType);
			}
		}
		return method.Invoke(this, array);
	}

	public async Task<object> InvokeMethodAsync(MethodInfo method, Dictionary<string, string> parameters)
	{
		object obj = InvokeMethod(method, parameters);
		if (obj is Task taskResult)
		{
			await taskResult.ConfigureAwait(continueOnCapturedContext: false);
			if (taskResult.GetType().IsGenericType)
			{
				return taskResult.GetType().GetProperty("Result")?.GetValue(taskResult);
			}
			return null;
		}
		if (obj is ValueTask valueTaskResult)
		{
			await valueTaskResult.ConfigureAwait(continueOnCapturedContext: false);
			if (valueTaskResult.GetType().IsGenericType)
			{
				return valueTaskResult.GetType().GetProperty("Result")?.GetValue(valueTaskResult);
			}
			return null;
		}
		return obj;
	}

	private object GetDefaultValue(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			return null;
		}
		if (!type.IsValueType)
		{
			return null;
		}
		return Activator.CreateInstance(type);
	}

	private object ConvertValue(string value, Type targetType)
	{
		if (targetType == typeof(string))
		{
			return value;
		}
		if (targetType == typeof(bool))
		{
			if (bool.TryParse(value, out var result))
			{
				return result;
			}
			if (value == "1" || value.ToLower() == "yes")
			{
				return true;
			}
			if (value == "0" || value.ToLower() == "no")
			{
				return false;
			}
			throw new FormatException("无法将 '" + value + "' 转换为布尔值");
		}
		if (targetType.IsEnum)
		{
			return Enum.Parse(targetType, value, ignoreCase: true);
		}
		if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			Type underlyingType = Nullable.GetUnderlyingType(targetType);
			return ConvertValue(value, underlyingType);
		}
		return Convert.ChangeType(value, targetType);
	}

	private object BindComplexType(Type dtoType, Dictionary<string, string> parameters)
	{
		object obj = Activator.CreateInstance(dtoType);
		PropertyInfo[] properties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			string text = (propertyInfo.GetCustomAttribute<UriParamAttribute>()?.Name ?? propertyInfo.Name)?.ToLower();
			string value;
			if (!IsSimpleType(propertyInfo.PropertyType))
			{
				string nestedPrefix = text + ".";
				Dictionary<string, string> dictionary = parameters.Where((KeyValuePair<string, string> kv) => kv.Key.StartsWith(nestedPrefix)).ToDictionary((KeyValuePair<string, string> kv) => kv.Key.Substring(nestedPrefix.Length), (KeyValuePair<string, string> kv) => kv.Value);
				if (dictionary.Any())
				{
					propertyInfo.SetValue(obj, BindComplexType(propertyInfo.PropertyType, dictionary));
				}
			}
			else if (parameters.TryGetValue(text, out value) && !string.IsNullOrEmpty(value))
			{
				try
				{
					object value2 = ConvertValue(value, propertyInfo.PropertyType);
					propertyInfo.SetValue(obj, value2);
				}
				catch (Exception)
				{
				}
			}
		}
		return obj;
	}

	private bool IsSimpleType(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			type = Nullable.GetUnderlyingType(type);
		}
		if (!type.IsPrimitive && !(type == typeof(string)) && !(type == typeof(decimal)) && !(type == typeof(DateTime)) && !(type == typeof(TimeSpan)) && !(type == typeof(Guid)) && !(type == typeof(Uri)))
		{
			return type.IsEnum;
		}
		return true;
	}
}
