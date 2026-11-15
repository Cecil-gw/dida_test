namespace ticktick_WPF.Util;

public class TriggerValue
{
	public string Value { get; set; }

	public int Length { get; set; }

	public string Unit { get; set; }

	public long Ordinal => Length * GetUnitWeight(Unit);

	private static int GetUnitWeight(string unit)
	{
		return unit switch
		{
			"S" => 1, 
			"M" => 60, 
			"H" => 3600, 
			"W" => 86400, 
			"D" => 604800, 
			_ => 0, 
		};
	}
}
