namespace Models
{
    public class UnirRequest
    {
   
        /// The unique name of this unit.
        public string Name { get; }
        /// A description of this unit.
        public string Description { get;  }
        /// A URL for the website of this unit.
        public string Url { get;  }
        /// The ID of this unit's parent unit.
        public int ParentId { get;  }        
    }
}