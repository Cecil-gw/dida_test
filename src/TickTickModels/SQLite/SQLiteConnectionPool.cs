using System.Collections.Generic;

namespace SQLite;

internal class SQLiteConnectionPool
{
	private class Entry
	{
		public SQLiteConnectionString ConnectionString { get; private set; }

		public SQLiteConnectionWithLock Connection { get; private set; }

		public Entry(SQLiteConnectionString connectionString, SQLiteOpenFlags openFlags)
		{
			ConnectionString = connectionString;
			Connection = new SQLiteConnectionWithLock(connectionString, openFlags);
		}

		public void OnApplicationSuspended()
		{
			Connection.Dispose();
			Connection = null;
		}
	}

	private readonly Dictionary<string, Entry[]> _readOnlyEntries = new Dictionary<string, Entry[]>();

	private readonly Dictionary<string, int> _readOnlyEntryIndexes = new Dictionary<string, int>();

	private SQLiteConnectionWithLock _readRightConnection;

	private readonly object _entriesLock = new object();

	private static readonly SQLiteConnectionPool _shared = new SQLiteConnectionPool();

	public static SQLiteConnectionPool Shared => _shared;

	public SQLiteConnectionWithLock GetConnection(SQLiteConnectionString connectionString, SQLiteOpenFlags openFlags)
	{
		lock (_entriesLock)
		{
			if (openFlags == SQLiteOpenFlags.ReadOnly)
			{
				string connectionString2 = connectionString.ConnectionString;
				if (!_readOnlyEntries.TryGetValue(connectionString2, out var value))
				{
					value = new Entry[6];
					_readOnlyEntries[connectionString2] = value;
				}
				if (!_readOnlyEntryIndexes.ContainsKey(connectionString2))
				{
					_readOnlyEntryIndexes[connectionString2] = 0;
				}
				int num = _readOnlyEntryIndexes[connectionString2] % 6;
				Entry entry = value[num];
				if (entry == null)
				{
					entry = (value[num] = new Entry(connectionString, openFlags));
				}
				num++;
				num %= 6;
				_readOnlyEntryIndexes[connectionString2] = num;
				return entry.Connection;
			}
			if (_readRightConnection?.DatabasePath != connectionString.ConnectionString)
			{
				_readRightConnection = new SQLiteConnectionWithLock(connectionString, openFlags);
			}
			return _readRightConnection;
		}
	}

	public void Reset()
	{
		lock (_entriesLock)
		{
			foreach (Entry[] value in _readOnlyEntries.Values)
			{
				for (int i = 0; i < value.Length; i++)
				{
					value[i]?.OnApplicationSuspended();
				}
			}
			_readRightConnection.Dispose();
			_readRightConnection = null;
			_readOnlyEntries.Clear();
			_readOnlyEntryIndexes.Clear();
		}
	}

	public void ApplicationSuspended()
	{
		Reset();
	}
}
