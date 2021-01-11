using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class MemberTool : Entity
    {   
        /// The ID of the member in this relationship
        [Required]
        public int MembershipId { get; }
        /// The ID of the tool in this relationship
        [Required]
        public int ToolId { get; }
    }
        
}