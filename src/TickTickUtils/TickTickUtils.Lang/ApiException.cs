using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TickTickUtils.Lang;

public class ApiException : Exception
{
	public ApiError Error { get; set; }

	public string ErrorId => Error?.ErrorId;

	public string ErrorCode => Error?.ErrorCode;

	public string ErrorMessage => Error?.ErrorMessage;

	public ApiException(ApiError error)
		: base(error?.ErrorMessage)
	{
		Error = error ?? new ApiError();
		if (error == null || !(error.Data is JObject jObject))
		{
			return;
		}
		Dictionary<string, object> dictionary = jObject.ToObject<Dictionary<string, object>>();
		if (dictionary == null)
		{
			return;
		}
		IDictionary data = base.Data;
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			data.Add(item.Key, item.Value);
		}
	}

	public override string ToString()
	{
		ApiError error = Error;
		return "ApiException " + ((error == null) ? base.ToString() : error.ToString());
	}
}
