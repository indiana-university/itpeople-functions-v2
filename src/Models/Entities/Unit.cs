using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Unit : UnitResponse
    {
        /// The unique ID of the parent unit of this unit.
        public int? ParentId { get; set; } 
        /// The parent unit of this unit
        public Unit Parent { get; set; }
        public List<UnitMember> UnitMembers { get; set; }
        public List<SupportRelationship> SupportRelationships { get; set; }


        public Unit() {}
        public Unit(string name, string description, string url, string email, int? parentId = null)
        {
            Name = name;
            Description = description;
            Url = url;
            Email = email;
            ParentId = parentId;
        }
    }
    
}