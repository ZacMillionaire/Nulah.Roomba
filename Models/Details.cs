using System;
using System.Collections.Generic;
using System.Text;

namespace Nulah.Roomba.Models {
    public class Details {
        public string hostname { get; set; }
        public string robotname { get; set; }
        public string ip { get; set; }
        public string mac { get; set; }
        public string sw { get; set; }
        public string sku { get; set; }
        public int nc { get; set; }
        public string proto { get; set; }
        public Cap cap { get; set; }
    }
    /*
    public class Cap {
        public int pose { get; set; }
        public int ota { get; set; }
        public int multiPass { get; set; }
        public int carpetBoost { get; set; }
        public int pp { get; set; }
        public int binFullDetect { get; set; }
        public int langOta { get; set; }
        public int maps { get; set; }
        public int edge { get; set; }
        public int eco { get; set; }
        public int svcConf { get; set; }
    }
    */
}
