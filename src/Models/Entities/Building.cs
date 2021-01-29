namespace Models
{
    /// A university building or location
    public class Building : Entity
    {
        /// The name of this department.
        public string  Name { get; set;}
        /// A description or longer name of this department.
        public string  Code { get; set; }
        public string  Address { get; set; }
        public string City { get; set; }
        public string  State { get; set; }
        public string Country { get; set; }
        public string PostCode { get; set;}
    }
}