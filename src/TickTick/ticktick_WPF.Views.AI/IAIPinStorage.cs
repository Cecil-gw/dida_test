using System.Threading.Tasks;

namespace ticktick_WPF.Views.AI;

internal interface IAIPinStorage
{
	Task<string> ReadAsync(string scope);

	Task WriteAsync(string scope, string json);
}
