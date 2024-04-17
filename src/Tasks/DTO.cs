using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Models;

namespace Tasks
{
     public class DenodoResponse<T>
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("elements")] public IEnumerable<T> Elements { get; set; }
    }

    // BUILDINGS

    public class DenodoBuilding
    {
        [JsonPropertyName("building_name")] public string Name { get; set; }
        [JsonPropertyName("building_code")] public string BuildingCode { get; set; }
        [JsonPropertyName("site_code")] public string SiteCode { get; set; }
        [JsonPropertyName("building_long_description")] public string Description { get; set; }
        [JsonPropertyName("street")] public string Street { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("zip")] public string Zip { get; set; }

        public void MapToBuilding(Building record)
        {
            record.Name = string.IsNullOrWhiteSpace(Description) ? Name : Description;
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
        [JsonPropertyName("totalRecords")] public int TotalRecords { get; set; }
        [JsonPropertyName("currentPage")] public string CurrentPage { get; set; }
        [JsonPropertyName("lastPage")] public string LastPage { get; set; }
    }
    
        public class ProfileJob 
    { 
        [JsonPropertyName("jobStatus")] public string  JobStatus { get; set; }
        [JsonPropertyName("jobDepartmentId")] public string JobDepartmentId { get; set; }
        [JsonPropertyName("jobDepartmentDesc")] public string JobDepartmentDesc { get; set; }
        [JsonPropertyName("position")] public string  Position { get; set; }
    }

    public class ProfileContact
    { 
        [JsonPropertyName("phoneNumber")] public string PhoneNumber { get; set; }
        [JsonPropertyName("campusCode")] public string CampusCode { get; set; }
    }

    public class ProfileEmployee
    { 
        [JsonPropertyName("lastName")] public string LastName { get; set; }
        [JsonPropertyName("firstName")] public string FirstName { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("jobs")] public IEnumerable<ProfileJob> Jobs { get; set; }
        [JsonPropertyName("contacts")] public IEnumerable<ProfileContact>  Contacts { get; set; }

        public void MapToHrPerson(HrPerson record)
        {
            var contact = Contacts.FirstOrDefault();
            var job = Jobs.FirstOrDefault(j => j.JobStatus == "P"); // Primary job
            record.Name = $"{FirstName} {LastName}";
            record.NameFirst = FirstName;
            record.NameLast = LastName;
            record.Netid = Username.ToLowerInvariant();
            record.CampusEmail = Email;
            record.Campus = contact?.CampusCode == null ? "" : contact.CampusCode; 
            record.CampusPhone = contact?.PhoneNumber == null ? "" : contact.PhoneNumber; 
            record.Position = job == null ? "" : job.Position;
            record.HrDepartment = job == null ? "" : job.JobDepartmentId;
            record.HrDepartmentDescription = job == null ? "" : job.JobDepartmentDesc;
        }
    }
        
    public class ProfileResponse
    {
        [JsonPropertyName("page")] public ProfilePage  page { get; set; }
        [JsonPropertyName("employees")] public IEnumerable<ProfileEmployee> employees { get; set; }
        [JsonPropertyName("affiliates")] public IEnumerable<ProfileEmployee> affiliates { get; set; }
        [JsonPropertyName("foundations")] public IEnumerable<ProfileEmployee>  foundations { get; set; }
    }

}
