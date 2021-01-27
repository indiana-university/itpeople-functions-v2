using System.Net;
using System.Collections.Generic;

namespace Models
{
    public class ApiError
    {
        public int StatusCode { get; set; }
        public List<string> Errors { get; set; }
        public string Details { get; set; }
    }
}