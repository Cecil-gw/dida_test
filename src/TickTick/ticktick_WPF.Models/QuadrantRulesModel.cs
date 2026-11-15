namespace ticktick_WPF.Models;

public class QuadrantRulesModel
{
	public string Quadrant1 { get; set; }

	public string Quadrant2 { get; set; }

	public string Quadrant3 { get; set; }

	public string Quadrant4 { get; set; }

	public static QuadrantRulesModel GetDefault()
	{
		return new QuadrantRulesModel
		{
			Quadrant1 = "{\"and\":[{\"or\":[5],\"conditionName\":\"priority\",\"conditionType\":1}],\"type\":0,\"version\":1}",
			Quadrant2 = "{\"and\":[{\"or\":[3],\"conditionName\":\"priority\",\"conditionType\":1}],\"type\":0,\"version\":1}",
			Quadrant3 = "{\"and\":[{\"or\":[1],\"conditionName\":\"priority\",\"conditionType\":1}],\"type\":0,\"version\":1}",
			Quadrant4 = "{\"and\":[{\"or\":[0],\"conditionName\":\"priority\",\"conditionType\":1}],\"type\":0,\"version\":1}"
		};
	}
}
