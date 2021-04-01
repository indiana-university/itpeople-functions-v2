
namespace Models
{
    public class UaaJwtResponse {
        /// The OAuth JSON Web Token (JWT) that represents the logged-in user. The JWT must be passed in an HTTP Authentication header in the form: 'Bearer <JWT>'
        public string access_token { get; set; }
    }
}