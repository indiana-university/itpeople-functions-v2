using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Models
{
    public class MemberToolSlim : Entity
    {
        /// The ID of the member in this relationship
        [Required]
        public int MembershipId { get; set; }
        /// The ID of the tool in this relationship
        [Required]
        public int ToolId { get; set; }
    }

    public class MemberToolRequest : MemberToolSlim
    {        
    }

    public class MemberToolResponse : MemberToolSlim
    {
        public static List<MemberToolResponse> ConvertList(List<MemberTool> source)
            => source.Select(mt => mt.ToMemberToolResponse()).ToList();
    }

    [Table("unit_member_tools")]
    public class MemberTool : MemberToolSlim
    {
        [ForeignKey(nameof(MembershipId))]
        public UnitMember UnitMember { get; set; }

        public Tool Tool { get; set; }

        public MemberToolResponse ToMemberToolResponse() 
        => new MemberToolResponse()
            {
                Id = Id,
                MembershipId = MembershipId,
                ToolId = ToolId
            };
    }
        
}