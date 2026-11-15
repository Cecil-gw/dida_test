using System.Threading.Tasks;
using SQLite;

namespace ticktick_WPF.Views.AI;

internal sealed class AISqlitePinStorage : IAIPinStorage
{
	private readonly SQLiteAsyncConnection _connection;

	private SQLiteAsyncConnection Connection => _connection ?? App.Connection;

	internal AISqlitePinStorage(SQLiteAsyncConnection connection = null)
	{
		_connection = connection;
	}

	public async Task<string> ReadAsync(string scope)
	{
		await Connection.CreateTableAsync<AIPinStorageRow>();
		return (await (from row in Connection.Table<AIPinStorageRow>()
			where row.Scope == scope
			select row).FirstOrDefaultAsync())?.Json;
	}

	public async Task WriteAsync(string scope, string json)
	{
		await Connection.CreateTableAsync<AIPinStorageRow>();
		await Connection.RunInTransactionAsync(delegate(SQLiteConnection connection)
		{
			connection.InsertOrReplace(new AIPinStorageRow
			{
				Scope = scope,
				Json = json
			});
		});
	}
}
