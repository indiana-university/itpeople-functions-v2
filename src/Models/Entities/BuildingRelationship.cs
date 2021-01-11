namespace Models
{
    /// This relationship describes which IT Unit provides IT-related support for a given building.
    public class BuildingRelationship : Entity
    {
        
        /// The ID of the unit in this relationship
        public int UnitId { get; }
        /// The ID of the department in this relationship
        public int BuildingId { get; }
        /// The unit in this relationship.
        public Unit Unit { get; }
        /// The building in this relationship.
        public Building Building { get; }
  
    }
        
}