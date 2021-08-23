using System.Collections.Generic;
using Models;

namespace Models
{
	public static class TestEntities
	{
		public static class Buildings
		{
			public const int CityHallId = 1;

			public static readonly Building CityHall = new Building()
			{
				Id = CityHallId,
				Name = "Pawnee City Hall",
				Code = "PA123",
				Address = "123 Main St",
				City = "Pawnee",
				State = "IN",
				Country = "USA",
				PostCode = "47501"
			};
			public const int RonsCabinId = 2;

			public static readonly Building RonsCabin = new Building()
			{
				Id = RonsCabinId,
				Name = "Ron's Cabin",
				Code = "RC123",
				Address = "Out in the woods",
				City = "Swansonville",
				State = "IN",
				Country = "USA",
				PostCode = "47501"
			};

			public const int SmallParkId = 3;

			public static readonly Building SmallPark = new Building()
			{
				Id = SmallParkId,
				Name = "Smallest Park",
				Code = "PA1231",
				Address = "321 Main St",
				City = "Pawnee",
				State = "IN",
				Country = "USA",
				PostCode = "47501"
			};
		}

        
		public static class BuildingRelationships
		{
			public const int CityHallCityOfPawneeId = 1;

			public static readonly BuildingRelationship CityHallCityOfPawnee = new BuildingRelationship()
			{
				Id = CityHallCityOfPawneeId,
				UnitId = TestEntities.Units.CityOfPawnee.Id,
				BuildingId = TestEntities.Buildings.CityHall.Id,
				Unit = TestEntities.Units.CityOfPawnee,
				Building = TestEntities.Buildings.CityHall
			};
			public const int RonsCabinCityOfPawneeId = 2;

			public static readonly BuildingRelationship RonsCabinCityOfPawnee = new BuildingRelationship()
			{
				Id = RonsCabinCityOfPawneeId,
				UnitId = TestEntities.Units.CityOfPawnee.Id,
				BuildingId = TestEntities.Buildings.RonsCabin.Id,
				Unit = TestEntities.Units.CityOfPawnee,
				Building = TestEntities.Buildings.RonsCabin
			};
		}

		public static class Departments
        {
            public const int ParksId = 1;
            public const string ParksName = "Parks Department";
            public static readonly Department Parks = new Department() { 
                Id = ParksId, 
                Name = ParksName, 
                Description = "Your local Parks department." 
            };
            public const int FireId = 2;
            public const string FireName = "Fire Department";
            public static readonly Department Fire = new Department() { 
                Id = FireId, 
                Name = FireName, 
                Description = "Your local fire department." 
            };
            public const int AuditorId = 3;
            public const string AuditorName = "Auditor";
            public static readonly Department Auditor = new Department() { 
                Id = AuditorId, 
                Name = AuditorName, 
                Description = "Your local auditor's department." 
            };
        }

		public static class HrPeople
		{
			public const int Tammy1Id = 1;

			public static readonly HrPerson Tammy1 = new HrPerson()
			{
				Id = Tammy1Id,
				Netid = "tswanson1",
				Name = "Swanson, Tammy1",
				NameFirst = "Tammy1",
				NameLast = "Swanson",
				Position = "Ron's 1st ex-wife",
				Campus = "Pawnee",
				CampusPhone = "",
				CampusEmail = "tswanson1@pawnee.in.us",
				HrDepartment = TestEntities.Departments.Auditor.Name,
				HrDepartmentDescription = TestEntities.Departments.Auditor.Description
			};
		}

		public static class MemberTools
		{
			public const int RonHammerId = 1;
			public static readonly MemberTool MemberTool = new MemberTool()
			{
				Id = RonHammerId,
				MembershipId = UnitMembers.RSwansonLeaderId,
				ToolId = Tools.HammerId
			};

			public const int AdminHammerId = 2;
			public static readonly MemberTool AdminMemberTool = new MemberTool()
			{
				Id = AdminHammerId,
				MembershipId = UnitMembers.AdminMemberId,
				ToolId = Tools.HammerId
			};
		}
		public static class People
		{
			public const int RSwansonId = 1;
			public static readonly Person RSwanson = new Person()
			{
				Id = RSwansonId,
				Netid = "rswanso",
				Name = "Swanson, Ron",
				NameFirst = "Ron",
				NameLast = "Swanson",
				Position = "Parks and Rec Director",
				Location = "",
				Campus = "Pawnee",
				CampusPhone = "812.856.1111",
				CampusEmail = "rswanso@pawnee.in.us",
				Expertise = "Woodworking; Honor",
				Notes = "",
				PhotoUrl = "http://flavorwire.files.wordpress.com/2011/11/ron-swanson.jpg",
				Responsibilities = Responsibilities.ItLeadership,
				DepartmentId = Departments.Parks.Id,
				Department = Departments.Parks,
				IsServiceAdmin = false
			};

			public const int LKnopeId = 2;
			public static readonly Person LKnope = new Person()
			{
				Id = LKnopeId,
				Netid = "lknope",
				Name = "Knope, Leslie",
				NameFirst = "Leslie",
				NameLast = "Knope",
				Position = "Parks and Rec Deputy Director",
				Location = "",
				Campus = "Pawnee",
				CampusPhone = "812.856.2222",
				CampusEmail = "lknope@pawnee.in.us",
				Expertise = "Canvassing; Waffles",
				Notes = "",
				PhotoUrl = "https://en.wikipedia.org/wiki/Leslie_Knope#/media/File:Leslie_Knope_(played_by_Amy_Poehler).png",
				Responsibilities = Responsibilities.ItLeadership | Responsibilities.ItProjectMgt,
				DepartmentId = Departments.Parks.Id,
				Department = Departments.Parks,
				IsServiceAdmin = false
			};

			public const int BWyattId = 3;
			public static readonly Person BWyatt = new Person()
			{
				Id = BWyattId,
				Netid = "bwyatt",
				Name = "Wyatt, Ben",
				NameFirst = "Ben",
				NameLast = "Wyatt",
				Position = "Auditor",
				Location = "",
				Campus = "Indianapolis",
				CampusPhone = "317.441.3333",
				CampusEmail = "bwyatt@pawnee.in.us",
				Expertise = "Board Games; Comic Books",
				Notes = "",
				PhotoUrl = "https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg",
				Responsibilities = Responsibilities.ItProjectMgt,
				DepartmentId = Departments.Auditor.Id,
				Department = Departments.Auditor,
				IsServiceAdmin = false
			};

			public const int ServiceAdminId = 4;
			public const string ServiceAdminEmail = "admin@pawnee.in.us";
			public static readonly Person ServiceAdmin = new Person()
			{
				Id = ServiceAdminId,
				Netid = "johndoe",
				Name = "Bureaucrat, Faceless",
				NameFirst = "Faceless",
				NameLast = "Bureaucrat",
				Position = "BOH",
				Location = "",
				Campus = "Pawnee",
				CampusPhone = "812.856.4444",
				CampusEmail = ServiceAdminEmail,
				Expertise = "Guarding the Precious",
				Notes = "",
				PhotoUrl = "",
				Responsibilities = Responsibilities.ItSecurityPrivacy,
				DepartmentId = Departments.Parks.Id,
				Department = Departments.Parks,
				IsServiceAdmin = true
			};
		}

		public static class SupportRelationships
		{
			public const int ParksAndRecRelationshipId = 1;

			public static readonly SupportRelationship ParksAndRecRelationship = new SupportRelationship()
			{
				Id = ParksAndRecRelationshipId,
				UnitId = TestEntities.Units.CityOfPawnee.Id,
				DepartmentId = TestEntities.Departments.Parks.Id,
				Unit = TestEntities.Units.CityOfPawnee,
				Department = TestEntities.Departments.Parks,
				SupportTypeId = SupportTypes.FullService.Id,
				SupportType = SupportTypes.FullService
			};
			public const int PawneeUnitFireId = 2;

			public static readonly SupportRelationship PawneeUnitFire = new SupportRelationship()
			{
				Id = PawneeUnitFireId,
				UnitId = TestEntities.Units.CityOfPawnee.Id,
				DepartmentId = TestEntities.Departments.Fire.Id,
				Unit = TestEntities.Units.CityOfPawnee,
				Department = TestEntities.Departments.Fire,
				SupportTypeId = SupportTypes.FullServiceId,
				SupportType = SupportTypes.FullService
			};
		}

		public static class Tools
		{
            public const int HammerId = 2;
            public static readonly Tool Hammer = new Tool()
            {
                Id = HammerId,
                Name = "Hammer",
                Description = "Claw Hammer",
                DepartmentScoped = true,
                ADPath = ""
            };

			public const int SawId = 1;
			public static readonly Tool Saw = new Tool
			{
				Id = SawId,
				Name = "Ron's Saw",
				Description = "Ron's prized Golden Guinea push saw.",
				DepartmentScoped = true,
				ADPath = "pw-parksrec-carpenters"
			};
		}

		public static class Units
		{
			public const int CityOfPawneeUnitId = 1;
			public const string CityOfPawneeEmail = "city@pawnee.in.us";
			public static readonly Unit CityOfPawnee = new Unit()
			{
				Id = CityOfPawneeUnitId,
				Name = "City of Pawnee",
				Description = "City of Pawnee, Indiana",
				Url = "http://pawneeindiana.com/",
				Email = CityOfPawneeEmail,
				ParentId = null,
				Parent = null
			};
			public const int ParksAndRecUnitId = 2;
			public static readonly Unit ParksAndRecUnit = new Unit()
			{
				Id = ParksAndRecUnitId,
				Name = "Parks and Rec",
				Description = "Parks and Recreation",
				Url = "http://pawneeindiana.com/parks-and-recreation/",
				Email = "unit@example.com",
				ParentId = CityOfPawnee.Id,
				Parent = CityOfPawnee
			};
			public const int AuditorId = 3;

			public static readonly Unit Auditor = new Unit()
			{
				Id = AuditorId,
				Name = "Auditor",
				Description = "City Auditors",
				Url = "http://pawneeindiana.com/auditor/",
				Email = "auditor@example.com",
				ParentId = CityOfPawnee.Id,
				Parent = CityOfPawnee
			};
		}

		public static class UnitMembers
		{
			public const int RSwansonLeaderId = 1;
			public static readonly UnitMember RSwansonDirector = new UnitMember()
			{
				Id = RSwansonLeaderId,
				Role = Role.Leader,
				Permissions = UnitPermissions.Owner,
				PersonId = People.RSwansonId,
				Title = "Director",
				Percentage = 100,
				Notes = "",
				Person = People.RSwanson,
				Unit = Units.ParksAndRecUnit,
				UnitId = Units.ParksAndRecUnitId,
				MemberTools = new List<MemberTool>()
			};
			public const int LkNopeSubleadId = 2;
			public static readonly UnitMember LkNopeSublead = new UnitMember()
			{
				Id = LkNopeSubleadId,
				Role = Role.Sublead,
				Permissions = UnitPermissions.Viewer,
				PersonId = People.LKnopeId,
				Title = "Deputy Director",
				Percentage = 100,
				Notes = "Definitely going to run for office some day.",
				Person = People.LKnope,
				Unit = Units.ParksAndRecUnit,
				UnitId = Units.ParksAndRecUnitId,
				MemberTools = null
			};
			public const int BWyattMemberId = 3;
			public static readonly UnitMember BWyattAditor = new UnitMember()
			{
				Id = BWyattMemberId,
				Role = Role.Member,
				Permissions = UnitPermissions.ManageMembers,
				PersonId = People.BWyattId,
				Title = "Auditor",
				Percentage = 100,
				Notes = "notes about Ben",
				Person = People.BWyatt,
				Unit = Units.Auditor,
				UnitId = Units.AuditorId,
				MemberTools = null
			};
			public const int AdminMemberId = 4;
			public static readonly UnitMember AdminLeader = new UnitMember()
			{
				Id = AdminMemberId,
				Role = Role.Leader,
				Permissions = UnitPermissions.ManageMembers,
				PersonId = People.ServiceAdminId,
				Title = "Adm",
				Percentage = 100,
				Notes = "notes",
				Person = People.ServiceAdmin,
				Unit = Units.CityOfPawnee,
				UnitId = Units.CityOfPawneeUnitId,
				MemberTools = new List<MemberTool>(){MemberTools.AdminMemberTool}
			};
		}

		public static class SupportTypes
		{
			public const int FullServiceId = 1;
			public static readonly SupportType FullService = new SupportType()
			{
				Id = FullServiceId,
				Name = "Full Service"
			};
			public const int DesktopEndpointId = 2;
			public static readonly SupportType DesktopEndpoint = new SupportType()
			{
				Id = DesktopEndpointId,
				Name = "Desktop/Endpoint"
			};
			public const int WebAppInfrastructureId = 3;
			public static readonly SupportType WebAppInfrastructure = new SupportType()
			{
				Id = WebAppInfrastructureId,
				Name = "Web/app Infrastructure"
			};
			public const int ResearchInfrastructureId = 4;
			public static readonly SupportType ResearchInfrastructure = new SupportType()
			{
				Id = ResearchInfrastructureId,
				Name = "Research Infrastructure"
			};
		}
	}
}