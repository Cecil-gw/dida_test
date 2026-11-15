using System.Net;

namespace TickTickUtils.Lang;

public class ApiError
{
	private string _traceId;

	private string _code;

	private string _msg;

	public HttpStatusCode StatusCode { get; set; }

	public string ErrorId { get; set; }

	public string ErrorCode { get; set; }

	public string ErrorMessage { get; set; }

	public object Data { get; set; }

	public string TraceId
	{
		get
		{
			return _traceId ?? ErrorId;
		}
		set
		{
			_traceId = value;
		}
	}

	public string Code
	{
		get
		{
			return _code ?? ErrorCode;
		}
		set
		{
			_code = value;
		}
	}

	public string Msg
	{
		get
		{
			return _msg ?? ErrorMessage;
		}
		set
		{
			_msg = value;
		}
	}

	public override string ToString()
	{
		return $"{{Code: {Code}, Msg: {Msg}, ErrorId: {ErrorId}, StatusCode: {StatusCode}, TraceId: {TraceId}, Data: {Data} }}";
	}
}
