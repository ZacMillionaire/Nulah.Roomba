using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Nulah.Roomba.Models {

    public class MqttMessagePayload {
        public MqttMessage[] Messages { get; set; }
    }

    public class MqttMessage {
        public string Topic { get; set; }
        public Type Type { get; set; }
        public object Payload { get; set; }
        public string Raw { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
