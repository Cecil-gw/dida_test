using Newtonsoft.Json;

public class BackgroundModel
{
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "landscape")]
	public CropModel Landscape { get; set; }

	[JsonProperty(PropertyName = "portrait")]
	public CropModel Portrait { get; set; }
}
