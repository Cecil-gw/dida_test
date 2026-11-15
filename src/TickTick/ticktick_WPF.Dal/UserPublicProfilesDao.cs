using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public class UserPublicProfilesDao
{
	public static async Task<UserPublicProfilesModel> GetUserInfoByUserCode(string userCode)
	{
		UserPublicProfilesModel userPublicProfilesModel = await GetUserInfoByUsercode(userCode);
		if (userPublicProfilesModel == null)
		{
			userPublicProfilesModel = await UpdateUser(userCode);
		}
		return userPublicProfilesModel;
	}

	private static async Task<UserPublicProfilesModel> UpdateUser(string userCode)
	{
		UserPublicProfilesModel userPublicProfilesModel = await Communicator.GetUserPublicProfiles(userCode);
		if (userPublicProfilesModel != null)
		{
			UpdateOrInsertUser(userPublicProfilesModel);
		}
		return userPublicProfilesModel;
	}

	public static async Task<List<UserPublicProfilesModel>> GetUsersInfoByUserCodes(List<string> userCodes)
	{
		return await UpdateUsers(userCodes);
	}

	private static async Task<List<UserPublicProfilesModel>> UpdateUsers(List<string> userCodes)
	{
		List<UserPublicProfilesModel> list = await Communicator.GetUsersPublicProfiles(userCodes);
		if (list != null)
		{
			UpdateOrInsertUsers(list);
		}
		return list;
	}

	private static async Task<UserPublicProfilesModel> GetUserInfoByUsercode(string userCode)
	{
		return await Task.Run(async delegate
		{
			List<UserPublicProfilesModel> list = await (from v in App.Connection.Table<UserPublicProfilesModel>()
				where v.userCode == userCode
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task<UserPublicProfilesModel> GetUserInfoById(long userId)
	{
		return await Task.Run(async delegate
		{
			List<UserPublicProfilesModel> list = await (from v in App.Connection.Table<UserPublicProfilesModel>()
				where v.userId == (long?)userId
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task UpdateOrInsertUser(UserPublicProfilesModel user)
	{
		try
		{
			List<UserPublicProfilesModel> list = await (from v in App.Connection.Table<UserPublicProfilesModel>()
				where v.userCode.Equals(user.userCode)
				select v).ToListAsync();
			if (list.Count != 0)
			{
				user._Id = list[0]._Id;
				user.userId = (list[0].userId.HasValue ? list[0].userId : user.userId);
				await App.Connection.UpdateAsync(user);
			}
			else
			{
				await App.Connection.InsertAsync(user);
			}
		}
		catch (Exception)
		{
		}
	}

	public static async Task InsertUser(UserPublicProfilesModel user)
	{
		try
		{
			if ((await (from v in App.Connection.Table<UserPublicProfilesModel>()
				where v.userCode.Equals(user.userCode)
				select v).ToListAsync()).Count == 0)
			{
				await App.Connection.InsertAsync(user);
			}
		}
		catch (Exception)
		{
		}
	}

	private static async Task UpdateOrInsertUsers(List<UserPublicProfilesModel> users)
	{
		foreach (UserPublicProfilesModel user in users)
		{
			UpdateOrInsertUser(user);
		}
	}
}
