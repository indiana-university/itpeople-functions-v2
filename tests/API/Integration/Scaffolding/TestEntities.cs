using Models;

namespace Integration
{
    public static class TestEntities
    {
        public static class Departments
        {
            public static readonly Department Parks = new Department() { 
                Id = 1, 
                Name = "Parks Department", 
                Description = "Your local Parks department." 
            };
        }

        public static class People
        {
            public static readonly Person RSwanson = new Person() { 
                Id = 1, 
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
            public static readonly Person LKnope = new Person() { 
                Id = 2, 
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
            public static readonly Person BWyatt = new Person() { 
                Id = 3, 
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
        }
    }
}