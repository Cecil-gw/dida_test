using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ticktick_WPF.Util.Sync;

public class SyncTaskBeanLimitPropsContractResolver : DefaultContractResolver
{
	private string[] props;

	private bool retain;

	public SyncTaskBeanLimitPropsContractResolver(string[] props, bool retain = true)
	{
		this.props = props;
		this.retain = retain;
	}

	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
	{
		return (from p in base.CreateProperties(type, memberSerialization)
			where retain ? props.Contains<string>(p.PropertyName) : (!props.Contains<string>(p.PropertyName))
			select p).ToList();
	}
}
