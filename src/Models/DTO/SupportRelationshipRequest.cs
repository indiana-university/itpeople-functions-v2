using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SupportRelationshipRequest
    { 
        /// The ID of the unit in this relationship
        public int  UnitId { get; set; }
        /// The ID of the department in this relationship
        public int  DepartmentId { get; set; }
        /// The ID of the Unit to use as the department's ReportSupportingUnitId
        public int ReportSupportingUnitId { get; set; }
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

		private UnitResponse _ReportSupportingUnit;
		[Required]
		public UnitResponse ReportSupportingUnit
		{
			get => _ReportSupportingUnit;
			set
			{
				_ReportSupportingUnit = value;
				ReportSupportingUnitId = _ReportSupportingUnit?.Id ?? 0;
			}
		}
	}
}