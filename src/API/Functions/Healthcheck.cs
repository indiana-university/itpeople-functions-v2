using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Middleware;
using System.Threading.Tasks;
using System.Net;
using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;

namespace API.Functions
{
    public static class HealthCheck
    {
        [FunctionName(nameof(HealthCheck.Ping))]
        public static Task<IActionResult> Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => Response.Ok(req, Pipeline.Success("Pong!"));
        
        [FunctionName(nameof(HealthCheck.DnsCheck))]
        public static Task<IActionResult> DnsCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dnscheck")] HttpRequest req)
        {
            var output = "start of output\n";
            var success = true;
			
			RunDnsRequest(ref output, ref success, "itpeople.iu.edu", "8.8.8.8");
			RunDnsRequest(ref output, ref success, "itpeople.iu.edu", "129.79.1.1");
			RunDnsRequest(ref output, ref success, "esdbp57p.uits.iu.edu", "129.79.1.1");
            
            if(success == false)
            {
                return Task.FromResult(Response.ContentResponse(req, HttpStatusCode.BadRequest, "text/plain", output));
            }

            //return Response.Ok(req, Pipeline.Success(output));
            return Task.FromResult(Response.ContentResponse(req, HttpStatusCode.OK, "text/plain", output));
        }

        private static void RunDnsRequest(ref string output, ref bool success, string hostname, string dnsServerIp, int dnsServerPort = 53)
		{
			//var output = $"Resolve {hostname} using {dnsServerIp}:{dnsServerPort}... ";
            output += $"Resolve {hostname} using {dnsServerIp}:{dnsServerPort}... ";
			try
			{
				var result = DnsRequest(hostname, dnsServerIp, dnsServerPort);
				if(result.AddressList.Length == 0)
				{
                    success = false;
					throw new Exception("No A records were returned.");
				}
				output += $"Got {string.Join(", ", result.AddressList.Select(a => a.ToString()))}";
			}
			catch(Exception e)
			{
				success = false;
                output += "\n\t" + e.Message;
			}

			output += "\n";
		}

		private static IPHostEntry DnsRequest(string hostname, string dnsServerIp, int dnsServerPort = 53)
		{
			// Borrowing how to make a rudimentary DNS request from https://stackoverflow.com/a/47277960
			IPAddress dnsAddr;
			if (!IPAddress.TryParse(dnsServerIp, out dnsAddr))
			{
				throw new ArgumentException("The dns host must be ip address.", nameof(dnsServerIp));
			}
			using (MemoryStream ms = new MemoryStream())
			{
				Random rnd = new Random();
				//About the dns message:http://www.ietf.org/rfc/rfc1035.txt

				//Write message header.
				ms.Write(new byte[] {
					(byte)rnd.Next(0, 0xFF),(byte)rnd.Next(0, 0xFF),
					0x01,
					0x00,
					0x00,0x01,
					0x00,0x00,
					0x00,0x00,
					0x00,0x00
				}, 0, 12);

				//Write the host to query.
				foreach (string block in hostname.Split('.'))
				{
					byte[] data = Encoding.UTF8.GetBytes(block);
					ms.WriteByte((byte)data.Length);
					ms.Write(data, 0, data.Length);
				}
				ms.WriteByte(0);//The end of query, muest 0(null string)

				//Query type:A
				ms.WriteByte(0x00);
				ms.WriteByte(0x01);

				//Query class:IN
				ms.WriteByte(0x00);
				ms.WriteByte(0x01);

				Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
				try
				{
					//send to dns server
					byte[] buffer = ms.ToArray();
					while (socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, new IPEndPoint(dnsAddr, dnsServerPort)) < buffer.Length) ;
					// buffer = new byte[0x100];
					buffer = new byte[0x1000];
					EndPoint ep = socket.LocalEndPoint;
					int num = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref ep);

					//var str = System.Text.Encoding.Default.GetString(buffer);
					//Console.WriteLine(str);

					//The response message has the same header and question structure, so we move index to the answer part directly.
					int index = (int)ms.Length;
					//Parse response records.
					void SkipName()
					{
						while (index < num)
						{
							int length = buffer[index++];
							if (length == 0)
							{
								return;
							}
							else if (length > 191)
							{
								return;
							}
							index += length;
						}
					}

					List<IPAddress> addresses = new List<IPAddress>();
					while (index < num)
					{
						SkipName();//Seems the name of record is useless in this scense, so we just needs to get the next index after name.
						byte type = buffer[index += 2];
						index += 7;//Skip class and ttl
						if(index >= buffer.Length)
						{
							break;
						}
						int length = buffer[index++] << 8 | buffer[index++];//Get record data's length

						if (type == 0x01)//A record
						{
							if (length == 4)//Parse record data to ip v4, this is what we need.
							{
								addresses.Add(new IPAddress(new byte[] { buffer[index], buffer[index + 1], buffer[index + 2], buffer[index + 3] }));
							}
						}
						index += length;
					}
					return new IPHostEntry { AddressList = addresses.ToArray() };
				}
				finally
				{
					socket.Dispose();
				}
			}
		}
	}
}
