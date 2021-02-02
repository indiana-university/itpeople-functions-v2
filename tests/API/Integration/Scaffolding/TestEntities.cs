using System.Collections.Generic;
using Models;

namespace Integration
{
    public static class TestEntities
    {

        public static class Buildings {
            public const int CityHallId = 1;

            public static readonly Building CityHall = new Building() {
                Id=1,
                Name="Pawnee City Hall",
                Code="PA123",
                Address="123 Main St",
                City="Pawnee",
                State="IN",
                Country="USA",
                PostCode="47501"
            };
            public static readonly Building RonsCabin = new Building() {
                Id=2,
                Name="Ron's Cabin",
                Code="RC123",
                Address="Out in the woods",
                City="Swansonville",
                State="IN",
                Country="USA",
                PostCode="47501"
            };
            public static readonly Building SmallPark = new Building() {
                Id=3,
                Name="Smallest Park",
                Code="PA1231",
                Address="321 Main St",
                City="Pawnee",
                State="IN",
                Country="USA",
                PostCode="47501"
            };
        }

        public static class BuildingRelationships {
            public static readonly BuildingRelationship CityHallCityOfPawnee = new BuildingRelationship() {
                Id=1,
                UnitId = TestEntities.Units.CityOfPawnee.Id,
                BuildingId=TestEntities.Buildings.CityHall.Id,
                Unit=TestEntities.Units.CityOfPawnee,
                Building=TestEntities.Buildings.CityHall
            };
        }

        public static class Departments
        {
            public const int ParksId = 1;
            public static readonly Department Parks = new Department() { 
                Id = 1, 
                Name = "Parks Department", 
                Description = "Your local Parks department." 
            };
            public static readonly Department Fire = new Department() { 
                Id = 2, 
                Name = "Fire Department", 
                Description = "Your local fire department." 
            };
            public static readonly Department Health = new Department() { 
                Id = 3, 
                Name = "Health Department", 
                Description = "Your local health department." 
            };
        }

        public static class People
        {
            public const int RSwansonId = 1;
            public static readonly Person RSwanson = new Person() { 
                Id = RSwansonId, 
                Netid = "rswanson", 
                Name="Swanson, Ron", 
                NameFirst = "Ron", 
                NameLast = "Swanson", 
                Position = "Parks and Rec Director", 
                Location = "", 
                Campus = "Pawnee", 
                CampusPhone = "", 
                CampusEmail = "rswanso@pawnee.in.us", 
                Expertise = "Woodworking; Honor", 
                Notes = "", 
                PhotoUrl = "http://flavorwire.files.wordpress.com/2011/11/ron-swanson.jpg", 
                Responsibilities = Responsibilities.ItLeadership, 
                DepartmentId = Departments.Parks.Id, 
                Department = Departments.Parks, 
                IsServiceAdmin = false };

            public const int LKnopeId = 2;
            public static readonly Person LKnope = new Person() { 
                Id = LKnopeId, 
                Netid = "lknope", 
                Name="Knope, Leslie", 
                NameFirst = "Leslie", 
                NameLast = "Knope", 
                Position = "Parks and Rec Deputy Director", 
                Location = "", 
                Campus = "Pawnee", 
                CampusPhone = "", 
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
            public static readonly Person BWyatt = new Person() { 
                Id = BWyattId, 
                Netid = "bwyatt", 
                Name="Wyatt, Ben", 
                NameFirst = "Ben", 
                NameLast = "Wyatt", 
                Position = "Auditor", 
                Location = "", 
                Campus = "Indianapolis", 
                CampusPhone = "", 
                CampusEmail = "bwyatt@pawnee.in.us", 
                Expertise = "Board Games; Comic Books", 
                Notes = "", 
                PhotoUrl = "https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg", 
                Responsibilities = Responsibilities.ItProjectMgt, 
                DepartmentId = Departments.Parks.Id, 
                Department = Departments.Parks, 
                IsServiceAdmin = false 
            };

            public const int ServiceAdminId = 4;
            public static readonly Person ServiceAdmin = new Person() { 
                Id = ServiceAdminId, 
                Netid = "admin", 
                Name="Bureaucrat, Faceless", 
                NameFirst = "Faceless", 
                NameLast = "Bureaucrat", 
                Position = "BOH", 
                Location = "", 
                Campus = "Indianapolis", 
                CampusPhone = "", 
                CampusEmail = "admin@pawnee.in.us", 
                Expertise = "Guarding the Precious", 
                Notes = "", 
                PhotoUrl = "", 
                Responsibilities = Responsibilities.ItSecurityPrivacy, 
                DepartmentId = Departments.Parks.Id, 
                Department = Departments.Parks, 
                IsServiceAdmin = true 
            };
        }
        /*public static class MemberTools
        {
            public static readonly MemberTool MemberTool = new MemberTool() { 
                Id = 1, 
                MembershipId = 1, 
                ToolId = 1 
            };
            
        }*/

        public static class Units
        {
            public const int CityOfPawneeUnitId = 1;
            public static readonly Unit CityOfPawnee = new Unit(){
                Id = CityOfPawneeUnitId,
                Name = "City of Pawnee",
                Description = "City of Pawnee, Indiana",
                Url = "http://pawneeindiana.com/",
                Email = "city@pawnee.in.us",
                ParentId = null,
                Parent = null
            };
            public const int ParksAndRecUnitId = 2;
            public static readonly Unit ParksAndRecUnit = new Unit(){
                Id = ParksAndRecUnitId,
                Name = "Parks and Rec",
                Description = "Parks and Recreation",
                Url = "http://pawneeindiana.com/parks-and-recreation/",
                Email = "unit@example.com",
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
                //UnitId = Units.Unit.Id,
                Role = Role.Leader,
                Permissions = UnitPermissions.Owner,
                PersonId = People.RSwansonId,
                Title = "Director",
                Percentage = 100,
                Notes = "",
                //Netid = People.RSwanson.Netid,
                Person = People.RSwanson,
                Unit = Units.ParksAndRecUnit,
                MemberTools = new List<MemberTool> ()
            };
            public const int LkNopeSubleadId = 2;
            public static readonly UnitMember LkNopeSublead = new UnitMember()
            {
                Id = LkNopeSubleadId,
                // UnitId = Units.Unit.Id,
                Role = Role.Sublead,
                Permissions = UnitPermissions.Viewer,
                PersonId = People.LKnopeId,
                Title = "Deputy Director",
                Percentage = 100,
                Notes = "Office busy-body.",
                //Netid = People.LKnope.Netid,
                Person = People.LKnope,
                Unit = Units.ParksAndRecUnit,
                MemberTools = null
            };
            public const int BWyattMemberId = 3;
            public static readonly UnitMember BWyattAditor= new UnitMember()
            {
                Id = BWyattMemberId,
                // UnitId = Units.Unit.Id,
                Role = Role.Member,
                Permissions = UnitPermissions.ManageMembers,
                PersonId = People.BWyattId,
                Title = "Auditor",
                Percentage = 100,
                Notes = "",
                //Netid = People.BWyatt.Netid,
                Person = People.BWyatt,
                Unit = Units.CityOfPawnee,
                MemberTools = null
            };
            
        }
    }
}