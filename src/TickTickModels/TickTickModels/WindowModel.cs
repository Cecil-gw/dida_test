namespace TickTickModels;

public class WindowModel : BaseModel
{
	public string UserId { get; set; }

	public string Id { get; set; }

	public string Type { get; set; }

	public bool TopMost { get; set; }

	public bool MaxSize { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public double Left { get; set; }

	public double Top { get; set; }

	public string Data { get; set; }

	public bool Closed { get; set; }
}
