namespace Models
{
    public record Department : Entity
    {    
        /// The name of this department.
        public string Name { get; }
        /// A description or longer name of this department.
        public string Description { get; }
    }
}