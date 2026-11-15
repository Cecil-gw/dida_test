public class Data
{
	public string @params { get; set; }

	public string planCode { get; set; }

	public override string ToString()
	{
		return " params: " + @params + ",planCode: " + planCode;
	}
}
