namespace Models
{
    public enum UnitPermissions
    {
        /// This person has read/write permissions on this entity
        Owner=1,
        /// This person has read-only permissions on this entity
        Viewer=2,
        /// This person can modify unit membership/composition
        ManageMembers=3,
        /// This person can modify unit tools
        ManageTools=4   
    }

}