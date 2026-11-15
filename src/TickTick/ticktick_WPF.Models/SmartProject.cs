using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ticktick_WPF.Models;

public class SmartProject : IDroppable
{
	public virtual string Id { get; }

	public virtual string UserEventId { get; }

	public virtual string Name { get; }

	public virtual Geometry Icon { get; }

	public virtual string IconText => string.Empty;

	public int Count { get; set; }

	public virtual int SortOrder { get; set; }

	public virtual bool CanDrop => true;

	public virtual string ProjectId => null;

	public virtual DateTime? DefaultDate => null;

	public virtual bool IsCompleted => false;

	public virtual bool IsAbandoned => false;

	public virtual bool IsDeleted => false;

	public virtual List<string> Tags => new List<string>();

	public virtual int Priority => 0;

	public virtual bool Multiple => false;
}
