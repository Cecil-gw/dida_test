using System;
using Newtonsoft.Json.Linq;

namespace TickTickModels;

[Serializable]
public class ErrorModel : BaseModel
{
	public string errorId;

	public string errorCode;

	public string errorMessage;

	public string i18nMessage;

	public bool i18n;

	public JObject data;
}
