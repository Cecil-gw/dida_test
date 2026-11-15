using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ticktick_WPF.Views.AI;

internal sealed class AIMemoryPinStorage : IAIPinStorage
{
	private readonly ConcurrentDictionary<string, string> _rows = new ConcurrentDictionary<string, string>();

	public Task<string> ReadAsync(string scope)
	{
		string value;
		return Task.FromResult(_rows.TryGetValue(scope, out value) ? value : null);
	}

	public Task WriteAsync(string scope, string json)
	{
		_rows[scope] = json;
		return Task.CompletedTask;
	}
}
