using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SupportRelationshipRequest
    { 
        /// The ID of the unit in this relationship
        public int  UnitId { get; set; }
        /// The ID of the department in this relationship
        public int  DepartmentId { get; set; }
        /// The ID of the Unit to use as the department's PrimarySupportUnitId
        public int PrimarySupportUnitId { get; set; }
        /// The ID of the support type in this relationship
        public int? SupportTypeId { get; set; }
    }

    public class EnrichedSRR : SupportRelationshipRequest
	{
		private Models.UnitResponse _Unit;
		public Models.UnitResponse Unit
		{
			get => _Unit;
			set
			{
				_Unit = value;
				UnitId = _Unit.Id;
			}
		}

		private Department _Department;
		public Department Department
		{
			get => _Department;
			set
			{
				_Department = value;
				DepartmentId = _Department?.Id ?? 0;
			}
		}

		private SupportType _SupportType;
		public SupportType SupportType
		{
			get => _SupportType;
			set
			{
				_SupportType = value;
				SupportTypeId = _SupportType?.Id;
			}
		}

		private UnitResponse _PrimarySupportUnit;
		[Required]
		public UnitResponse PrimarySupportUnit
		{
			get => _PrimarySupportUnit;
			set
			{
				_PrimarySupportUnit = value;
				PrimarySupportUnitId = _PrimarySupportUnit?.Id ?? 0;
			}
		}
	}
}