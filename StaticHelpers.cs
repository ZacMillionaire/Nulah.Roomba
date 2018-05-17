using System;
using System.Collections.Generic;
using System.Text;

namespace Nulah.Roomba {
    public class StaticHelpers {
        public static long GetAgreedTimestamp() {
            return ( (DateTimeOffset)DateTime.UtcNow ).ToUnixTimeSeconds();
        }
    }
}
