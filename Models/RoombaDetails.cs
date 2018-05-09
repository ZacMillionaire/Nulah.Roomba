using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Nulah.Roomba.Models
{
    public class RoombaDetails
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public IPAddress LocalIp { get; set; }
        public Details Details { get; set; }
    }
}
