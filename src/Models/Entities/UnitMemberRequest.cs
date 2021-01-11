using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class UnitMemberRequest
    {
        [Required]
        public int UnitId { get; }
        /// The role of the person in this membership as part of the unit.
        [Required]
        public Role Role { get; }
        /// The permissions of the person in this membership as part of the unit. Defaults to 'viewer'.
        [DefaultValue(UnitPermissions.Viewer)]
        public UnitPermissions UnitPermissions { get; }
        /// The ID of the person class. This can be null if the position is vacant.
        public int? PersonId { get; }
        /// The NetId of the person, if they are not already in the IT people directory. This can be null if the position is vacant.
        public string NetId { get; }
        /// The title/position of this membership.
        public string Title { get; }
        /// The percentage of time allocated to this position by this person (in case of split appointments).
        [DefaultValue(100)]
        [Range(0,100)]
        public int Percentage { get; }
        /// Ad-hoc notes about this person's relationship to the unit, to be used by unit managers.
        public string Notes { get; }
    }
}