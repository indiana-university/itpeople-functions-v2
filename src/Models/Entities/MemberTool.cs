using System.ComponentModel.DataAnnotations;

namespace Models
{
    public record MemberTool : Entity
    {   
        /// The ID of the member in this relationship
        [Required]
        public int MembershipId { get; }
        /// The ID of the tool in this relationship
        [Required]
        public int ToolId { get; }
    }
        
}