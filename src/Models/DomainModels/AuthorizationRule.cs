using Models.Enums;

namespace Models
{
	public class AuthorizationRule
	{
		public EntityPermissions DefaultPermissions { get; set; } = EntityPermissions.Get;
		public EntityPermissions AdminPermissions { get; set; } = PermsGroups.All;
		public EntityPermissions OwnerPermissions { get; set; } = EntityPermissions.Get;
		public EntityPermissions ManageMemberPermissions { get; set; } = EntityPermissions.Get;
		public EntityPermissions ManageToolsPermissions { get; set; } = EntityPermissions.Get;
		public EntityPermissions ViewerPermissions { get; set; } = EntityPermissions.Get;
		public bool UnitPermissionsHeritable { get; set; } = true;

		public EntityPermissions GetEntityPermissions(UnitPermissions unitPermissions)
		{
			switch(unitPermissions)
			{
				case UnitPermissions.Owner:
					return OwnerPermissions;
				case UnitPermissions.ManageMembers:
					return ManageMemberPermissions;
				case UnitPermissions.ManageTools:
					return ManageToolsPermissions;
				case UnitPermissions.Viewer:
					return ViewerPermissions;
				default:
					return DefaultPermissions;
			}
		}
	}
}