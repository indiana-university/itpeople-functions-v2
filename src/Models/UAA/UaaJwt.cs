namespace Models
{
    public class UaaJwt{
        public string user_name { get; set; }
        // when this token expires, as a Unix timestamp
        public long exp { get; set; }
        // when this token becomes valid, as a Unix timestamp
        public long nbf { get; set; }
    }
}
