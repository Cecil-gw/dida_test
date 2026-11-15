namespace ticktick_WPF.UriScheme.Dto;

internal class AddTaskUriWithCallBackDto : AddTaskUriDto
{
	[UriParam("x-success")]
	public string xsuccess { get; set; }

	[UriParam("x-error")]
	public string xerror { get; set; }

	[UriParam("x-cancel")]
	public string xcancel { get; set; }
}
