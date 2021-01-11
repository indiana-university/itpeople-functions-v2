namespace Models
{
    public class Department : Entity
    {    
        /// The name of this department.
        public string Name { get; set; }
        /// A description or longer name of this department.
        public string Description { get; set; }
    }
}