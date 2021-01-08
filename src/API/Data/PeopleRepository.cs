using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Models;

namespace API.Data
{
    public class PeopleRepository
    {
        public PeopleRepository() 
        {
        }

        // /// Get a user record for a given net ID (e.g. 'jhoerr')
        // //TryGetId: NetId -> Async<Result<NetId * Id option,Error>>
        // public async Task<Result<(NetId, Id), option, Error>> TryGetId(string NetId) {
        //     throw new NotImplementedException();
        // }
        // /// Get a list of all people
        // public async Task<Result<List<Person>,Error>> GetAll(PeopleQuery peopleQuery) => throw new NotImplementedException();
        // /// Get a unioned list of IT and HR people, filtered by name/netid
        // public async Task<Result<List<Person>,Error>> GetAllWithHr(string NetId) => throw new NotImplementedException();
        // /// Get a single HR person by NetId
        // public async Task<Result<Person,Error>> GetHr(string NetId) => throw new NotImplementedException();
        // /// Get a single person by ID
        // public async Task<Result<Person,Error>> GetById(int Id) => throw new NotImplementedException();
        /// Get a single person by NetId
        public async Task<Result<Person,string>> GetByNetId(string NetId) => throw new NotImplementedException();
        // /// Create a person from canonical HR data
        // public  async Task<Result<Person,Error>> Create(Person person) => throw new NotImplementedException();
        // /// Get a list of a person's unit memberships, by the person's ID
        // public async Task<Result<List<UnitMember>,Error>> GetMemberships(Id id) => throw new NotImplementedException();
        
        // /// Update a person
        // public async Task<Result<Person,Error>> Update(PersonRequest personRequest) => throw new NotImplementedException();
    }
}