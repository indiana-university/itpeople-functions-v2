namespace Models
{
    public class HistoricalPersonUnitMetadata : Entity
    {
        public string Unit { get; }
        public int UnitId { get; }
        public string HrDepartment { get; }
        public Role Role { get; }
        public UnitPermissions Permissions { get; }
        public string Title { get; }
        public string Notes { get; } 
    }
}