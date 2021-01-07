namespace Models
{
    public record BuildingRelationshipRequest
    {
       
        /// The ID of the unit in this relationship
        public int UnitId { get; }       
        /// The ID of the building in this relationship
        public int BuildingId { get; }        

    }
    
}