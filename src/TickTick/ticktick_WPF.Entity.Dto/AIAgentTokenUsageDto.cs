using System;
using Newtonsoft.Json;

namespace ticktick_WPF.Entity.Dto;

public sealed class AIAgentTokenUsageDto
{
	[JsonProperty("updated_at")]
	public DateTimeOffset? UpdatedAt { get; set; }

	[JsonProperty("usage_percentage")]
	public double? UsagePercentage { get; set; }

	[JsonProperty("next_reset_date")]
	public DateTimeOffset? NextResetDate { get; set; }

	[JsonIgnore]
	public int DisplayPercentage => ToDisplayPercentage(UsagePercentage);

	internal static int ToDisplayPercentage(double? ratio)
	{
		if (!ratio.HasValue || double.IsNaN(ratio.Value) || ratio.Value <= 0.0)
		{
			return 0;
		}
		if (double.IsInfinity(ratio.Value) || ratio.Value >= 1.0)
		{
			return 100;
		}
		return (int)Math.Round(ratio.Value * 100.0, MidpointRounding.AwayFromZero);
	}
}
