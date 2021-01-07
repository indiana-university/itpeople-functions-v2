namespace Models
{
    public record PeopleQuery
    {
        public string Query { get; }
        public int Classes { get; }
        public string[] Interests { get; }
        public int[] Roles { get; }
        public int[] Permissions { get; }
        public string[] Campuses { get; }
        public int Area { get; }
    }
    
}