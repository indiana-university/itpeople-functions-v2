using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("unit_member_tools")]
    public class MemberTool : Entity
    {   
        /// The ID of the member in this relationship
        [Required]
        public int MembershipId { get; set; }
        /// The ID of the tool in this relationship
        [Required]
        public int ToolId { get; set; }

        [ForeignKey(nameof(MembershipId))]
        public UnitMember UnitMember { get; set; }

        public Tool Tool { get; set; }
    }
        
}