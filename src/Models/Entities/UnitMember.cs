namespace Models
{
    public class UnitMember : Entity
    {
        
        /// The ID of the unit class.
        public int UnitId { get; }
        /// The role of the person in this membership as part of the unit.
        public Role Role { get; }
        /// The permissions of the person in this membership as part of the unit. Defaults to 'viewer'.
        public UnitPermissions Permissions { get; set; }
        /// The ID of the person class. This can be null if the position is vacant.
        public Person PersonId { get; }    
        /// The title/position of this membership.
        public string Title { get; }   
        /// The percentage of time allocated to this position by this person (in case of split appointments).
        public int Percentage { get; }
        /// Notes about this person (for admins/reporting eyes only.)
        public string Notes { get; }
        /// The netid of the person related to this membership.
        public string NetId { get;  }
        /// The person related to this membership.
        public Person Person { get; }
        /// The unit related to this membership.
        public Unit Unit { get; }
        /// The tools that can be used by the person in this position as part of this unit.
        public MemberTool MemberTool { get;  }

    }
    
}