using System;

namespace web
{
	public class AuthenticatedUser
	{
		public string AccessToken {get;set;}
		public string Username {get;set;}
		public DateTimeOffset Expires {get; set;}

		public AuthenticatedUser() {}
		public AuthenticatedUser(string token, string username, string exp)
		{
			AccessToken = token;
			Username = username;
			// Convert exp from a unix epoch timestamp to a DateTimeOffset
			var expAsDouble = double.Parse(exp);
			Expires = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, new TimeSpan(0, 0, 0)).AddSeconds(expAsDouble);
		}
	}
}