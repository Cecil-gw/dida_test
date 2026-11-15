using Newtonsoft.Json;

public class CropModel
{
	[JsonProperty(PropertyName = "top")]
	public double Top { get; set; }

	[JsonProperty(PropertyName = "left")]
	public double Left { get; set; }

	[JsonProperty(PropertyName = "bottom")]
	public double Bottom { get; set; }

	[JsonProperty(PropertyName = "right")]
	public double Right { get; set; }
}
