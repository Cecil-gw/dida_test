using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class FilterRuleTest
{
	public const string TAG = "FilterRuleTest";

	public static void Test()
	{
		Test3();
	}

	public static void Test1()
	{
		PriorityRule priorityRule = new PriorityRule();
		priorityRule.and = new List<int> { 0, 1, 3, 5 };
		TagRule tagRule = new TagRule();
		tagRule.and = new List<string> { "aaa", "bbb" };
		Log.d("FilterRuleTest", JsonConvert.SerializeObject(priorityRule));
		Log.d("FilterRuleTest", JsonConvert.SerializeObject(tagRule));
	}

	public static void Test2()
	{
		NormalRuleModel normalRuleModel = new NormalRuleModel();
		PriorityRule priorityRule = new PriorityRule();
		priorityRule.or = new List<int> { 1, 3, 5 };
		AssigneeRule assigneeRule = new AssigneeRule();
		assigneeRule.or = new List<string> { "other", "noassignee" };
		normalRuleModel.and = new List<object>();
		normalRuleModel.and.Add(priorityRule);
		normalRuleModel.and.Add(assigneeRule);
		Log.d("FilterRuleTest", JsonConvert.SerializeObject(normalRuleModel));
	}

	public static void Test3()
	{
		NormalRuleModel normalRuleModel = new NormalRuleModel();
		normalRuleModel.and = new List<object>();
		ListOrGroupRule listOrGroupRule = new ListOrGroupRule();
		listOrGroupRule.or = new List<object>();
		ListRule listRule = new ListRule();
		listRule.or = new List<string> { "inbox1010557619" };
		GroupRule groupRule = new GroupRule();
		groupRule.or = new List<string> { "57b5519c7b854628f7e07622" };
		listOrGroupRule.or.Add(listRule);
		listOrGroupRule.or.Add(groupRule);
		normalRuleModel.and.Add(listOrGroupRule);
		Log.d("FilterRuleTest", JsonConvert.SerializeObject(normalRuleModel));
	}
}
