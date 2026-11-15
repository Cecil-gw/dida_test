namespace ticktick_WPF.Entity.Dto;

public class CommonDto<T>
{
	public int code { get; set; }

	public string msg { get; set; }

	public T data { get; set; }
}
