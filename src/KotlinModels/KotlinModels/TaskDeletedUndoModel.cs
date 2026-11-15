using System.Collections.Generic;

namespace KotlinModels;

public class TaskDeletedUndoModel
{
	public List<long> deleteIds = new List<long>();

	public List<string> deleteSids = new List<string>();

	public List<TaskDeleteUndoUpdateEntity> updateEntities = new List<TaskDeleteUndoUpdateEntity>();
}
