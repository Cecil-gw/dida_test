using System.Net;

namespace TickTickUtils.Lang;

public class HttpResponse<T>
{
	public HttpStatusCode StatusCode { get; }

	public T Content { get; }

	public bool IsSuccessStatusCode
	{
		get
		{
			_ = StatusCode;
			if (StatusCode >= HttpStatusCode.OK)
			{
				return StatusCode <= (HttpStatusCode)299;
			}
			return false;
		}
	}

	public HttpResponse(HttpStatusCode statusCode, T content)
	{
		StatusCode = statusCode;
		Content = content;
	}
}
