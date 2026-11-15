using System;
using System.Collections.Generic;

namespace SQLite;

public class CreateTablesResult
{
	public Dictionary<Type, int> Results { get; private set; }

	internal CreateTablesResult()
	{
		Results = new Dictionary<Type, int>();
	}
}
