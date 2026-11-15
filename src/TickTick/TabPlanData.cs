public class TabPlanData
{
	public string code { get; set; }

	public Data data { get; set; }

	public override string ToString()
	{
		return "code:" + code + ",data: " + data;
	}
}
