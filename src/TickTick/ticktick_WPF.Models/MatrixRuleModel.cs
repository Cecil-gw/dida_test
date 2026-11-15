namespace ticktick_WPF.Models;

public class MatrixRuleModel
{
	public QuadrantRulesModel Simple { get; set; }

	public QuadrantRulesModel Complex { get; set; }

	public QuadrantRulesModel Version0 { get; set; }

	public QuadrantRulesModel GetQuadrantRules(MatrixType type)
	{
		return type switch
		{
			MatrixType.Simple => Simple, 
			MatrixType.Version0 => Version0, 
			_ => Complex, 
		};
	}
}
