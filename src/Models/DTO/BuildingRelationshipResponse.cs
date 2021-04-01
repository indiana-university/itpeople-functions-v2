namespace Models
{
    /// This relationship describes which IT Unit provides IT-related support for a given building.
    public class BuildingRelationshipResponse : Entity
    {
        public BuildingRelationshipResponse()
        {
        }

        public BuildingRelationshipResponse(BuildingRelationship br)
        {
            Id = br.Id;
            UnitId = br.UnitId;
            BuildingId = br.BuildingId;
            Unit = new UnitResponse(br.Unit);
            Building = br.Building;
        }

        /// The ID of the unit in this relationship
        public int UnitId { get; set; }
        /// The ID of the department in this relationship
        public int BuildingId { get; set; }
        /// The unit in this relationship.
        public UnitResponse Unit { get; set; }
        /// The building in this relationship.
        public Building Building { get; set; }

    }
}