using System;

namespace Models.Enums
{
	[Flags]
	public enum EntityPermissions
	{
		Get = 0b0001,      // Fetch
		Post = 0b0010,      // Create
		Put = 0b0100,      // Update
		Delete = 0b1000,    // Remove
	}

	public static class PermsGroups
	{
		public const EntityPermissions All = EntityPermissions.Get | EntityPermissions.Post | EntityPermissions.Put | EntityPermissions.Delete;
		public const EntityPermissions GetPut = EntityPermissions.Get | EntityPermissions.Put;
		public const EntityPermissions GetPutDelete = EntityPermissions.Get | EntityPermissions.Post | EntityPermissions.Delete;
	}
}