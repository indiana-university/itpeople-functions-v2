using System.Collections.Generic;
using System.Linq;
using Models;
using Newtonsoft.Json;

namespace Tasks
{
     public class DenodoResponse<T>
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("elements")] public IEnumerable<T> Elements { get; set; }
    }

    // BUILDINGS

    public class DenodoBuilding
    {
        [JsonProperty("building_name")] public string Name { get; set; }
        [JsonProperty("building_code")] public string BuildingCode { get; set; }
        [JsonProperty("site_code")] public string SiteCode { get; set; }
        [JsonProperty("building_long_description")] public string Description { get; set; }
        [JsonProperty("street")] public string Street { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("zip")] public string Zip { get; set; }

        public void MapToBuilding(Building record)
        {
            record.Name = Description;
            record.Code = BuildingCode;
            record.Address = Street ?? "";
            record.City = City ?? "";
            record.State = State ?? "";
            record.PostCode = Zip ?? "";
            record.Country = "";
        }
    }

    // PEOPLE

    public class ProfilePage
    { 
        [JsonProperty("totalRecords")] public int TotalRecords { get; set; }
        [JsonProperty("currentPage")] public string CurrentPage { get; set; }
        [JsonProperty("lastPage")] public string LastPage { get; set; }
    }
    
        public class ProfileJob 
    { 
        [JsonProperty("jobStatus")] public string  JobStatus { get; set; }
        [JsonProperty("jobDepartmentId")] public string JobDepartmentId { get; set; }
        [JsonProperty("jobDepartmentDesc")] public string JobDepartmentDesc { get; set; }
        [JsonProperty("position")] public string  Position { get; set; }
    }

    public class ProfileContact
    { 
        [JsonProperty("phoneNumber")] public string PhoneNumber { get; set; }
        [JsonProperty("campusCode")] public string CampusCode { get; set; }
    }

    public class ProfileEmployee
    { 
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("username")] public string Username { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("jobs")] public IEnumerable<ProfileJob> Jobs { get; set; }
        [JsonProperty("contacts")] public IEnumerable<ProfileContact>  Contacts { get; set; }

        public void MapToHrPerson(HrPerson record)
        {
            var contact = Contacts.FirstOrDefault();
            var job = Jobs.FirstOrDefault(j => j.JobStatus == "P"); // Primary job
            record.Name = $"{FirstName} {LastName}";
            record.NameFirst = FirstName;
            record.NameLast = LastName;
            record.Netid = Username.ToLowerInvariant();
            record.CampusEmail = Email;
            record.Campus = contact == null ? "" : contact.CampusCode; 
            record.CampusPhone = contact == null ? "" : contact.PhoneNumber; 
            record.Position = job == null ? "" : job.Position;
            record.HrDepartment = job == null ? "" : job.JobDepartmentId;
            record.HrDepartmentDescription = job == null ? "" : job.JobDepartmentDesc;
        }
    }
        
    public class ProfileResponse
    {
        [JsonProperty("page")] public ProfilePage  page { get; set; }
        [JsonProperty("employees")] public IEnumerable<ProfileEmployee> employees { get; set; }
        [JsonProperty("affiliates")] public IEnumerable<ProfileEmployee> affiliates { get; set; }
        [JsonProperty("foundations")] public IEnumerable<ProfileEmployee>  foundations { get; set; }
    }

}
