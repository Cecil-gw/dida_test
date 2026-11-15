using System;
using System.Collections.Generic;

namespace ticktick_WPF.Models;

public interface IDroppable
{
	bool CanDrop { get; }

	string ProjectId { get; }

	DateTime? DefaultDate { get; }

	bool IsCompleted { get; }

	bool IsAbandoned { get; }

	List<string> Tags { get; }

	int Priority { get; }

	bool Multiple { get; }

	bool IsDeleted { get; }
}
