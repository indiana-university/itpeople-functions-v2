using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Unit : UnitResponse
    {
        /// The parent unit of this unit
        public new Unit Parent { get; set; }

        public List<UnitMember> UnitMembers { get; set; }
        public List<SupportRelationship> SupportRelationships { get; set; }
        public List<BuildingRelationship> BuildingRelationships { get; set; }


        public Unit() {
            Active = true;
        }
        public Unit(string name, string description, string url, string email, int? parentId = null, bool active = true)
        {
            Active = active;
            Name = name;
            Description = description;
            Url = url;
            Email = email;
            ParentId = parentId;
        }
    }
    
}