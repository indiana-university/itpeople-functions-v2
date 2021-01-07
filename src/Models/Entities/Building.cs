namespace Models
{
    /// A university building or location
    public record Building : Entity
    {
        /// The name of this department.
        public string  Name { get; }
        /// A description or longer name of this department.
        public string  Code { get; }
        public string  Address { get; }
        public string City { get; }
        public string  State { get; }
        public string Country { get; }
        public string PostCode { get;}
    }
}