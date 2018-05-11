using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nulah.Roomba.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Nulah.Roomba {
    // Most of this code is roughly ported from GetRoombaPassword in https://github.com/koalazak/dorita980
    public class Roomba980 {

        /// <summary>
        /// Returns the details of the Roomba at the specified IPAddress on the network.
        /// <para>
        /// This method will only succeed if the HOME button has been held down for several seconds until the Roomba beeps, and the wifi light flashes green.
        /// </para>
        /// </summary>
        /// <param name="RobotLocalIP"></param>
        /// <returns></returns>
        public RoombaDetails GetDetails(IPAddress RobotLocalIP) {
            var roombaDetails = GetRobotPublicInfo(RobotLocalIP);
            var roombaPassword = GetRoombaPassword(RobotLocalIP);
            return new RoombaDetails() {
                LocalIp = RobotLocalIP,
                Password = roombaPassword,
                Username = roombaDetails.hostname.Split('-').Last(),
                Details = roombaDetails
            };
        }

        public class RoombaReceivedMessageEvent : EventArgs {
            //public RoombaReceivedMessageEvent(MqttMessage m) {
            //    message = m;
            //}
            //private MqttMessage message;

            //public MqttMessage Message
            //{
            //    get { return message; }
            //    set { message = value; }
            //}
            public MqttMessage Message { get; set; }
        }

        public delegate void OnReceivedMessage(object sender, RoombaReceivedMessageEvent e);

        public event OnReceivedMessage OnMessage;

        /// <summary>
        /// Returns public information about the Roomba, including configuration settings.
        /// <para>
        /// This method can be called at any time, and does not require you to have the home button pressed to get a response.
        /// </para>
        /// </summary>
        /// <param name="RobotLocalIP"></param>
        public Details GetRobotPublicInfo(IPAddress RobotLocalIP) {
            var udpClient = new UdpClient();

            var msg = Encoding.ASCII.GetBytes("irobotmcs");

            udpClient.Send(msg, msg.Length, new IPEndPoint(RobotLocalIP, 5678));

            var res = udpClient.ReceiveAsync().Result;
            var roombaDetails = ParseBytesToType<Details>(res.Buffer);
            return roombaDetails;
        }

        /// <summary>
        /// Casts a byte[] to a given type, where it's known that the byte[] represents a json structure
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        private T ParseBytesToType<T>(byte[] byteArray) {
            var resString = Encoding.Default.GetString(byteArray);
            var deserialized = JsonConvert.DeserializeObject<T>(resString);
            return deserialized;
        }

        public List<string> types = new List<string>();

        private async Task<MqttMessage> ParseMQTTMessageToType(byte[] byteArray, string topic) {
            var resString = Encoding.Default.GetString(byteArray);
            Task<MqttMessage> res = Task.Factory.StartNew<MqttMessage>(() => {

                dynamic s = JsonConvert.DeserializeObject(resString);
                Newtonsoft.Json.Linq.JObject nestedObject = s.state.reported;
                var nestedPath = nestedObject.First.Path;
                //types.Add(nestedPath);

                MqttMessage mqttres = null;

                switch(nestedPath) {
                    case "state.reported.svcEndpoints":
                        mqttres = ToMqttMessage<Svcendpoints>("state.reported.svcEndpoints", nestedObject["svcEndpoints"]);
                        break;
                    case "state.reported.cloudEnv":
                        mqttres = ToMqttMessage<CloudEnv>("state.reported.cloudEnv", nestedObject);
                        break;
                    case "state.reported.country":
                        mqttres = ToMqttMessage<Country>("state.reported.country", nestedObject);
                        break;
                    case "state.reported.wifistat":
                        mqttres = ToMqttMessage<Wifistat>("state.reported.wifistat", nestedObject["wifistat"]);
                        break;
                    case "state.reported.netinfo":
                        mqttres = ToMqttMessage<Netinfo>("state.reported.netinfo", nestedObject["netinfo"]);
                        break;
                    case "state.reported.wlcfg":
                        mqttres = ToMqttMessage<Wlcfg>("state.reported.wlcfg", nestedObject["wlcfg"]);
                        break;
                    case "state.reported.mac":
                        mqttres = ToMqttMessage<Mac>("state.reported.mac", nestedObject);
                        break;
                    case "state.reported.localtimeoffset":
                        mqttres = ToMqttMessage<LocalTimeOffset>("state.reported.localtimeoffset", nestedObject);
                        break;
                    case "state.reported.batPct":
                        mqttres = ToMqttMessage<BatteryPercent>("state.reported.batPct", nestedObject);
                        break;
                    case "state.reported.cleanMissionStatus":
                        mqttres = ToMqttMessage<CleanMissionStatusSuper>("state.reported.cleanMissionStatus", nestedObject);
                        break;
                    case "state.reported.vacHigh":
                        mqttres = ToMqttMessage<VacHigh>("state.reported.vacHigh", nestedObject, new VacHighConverter());
                        break;
                    case "state.reported.bbnav":
                        // This report returns a lot along with it,
                        // They should probably be returned as seperate topics, but they get returned from the Roomba
                        // all at once so...idk, I've just rolled them up for simplicity until I know what
                        // exactly this refers to
                        mqttres = ToMqttMessage<BbNavSuper>("state.reported.bbnav", nestedObject);
                        break;
                    case "state.reported.bbrstinfo":
                        // Again, another rollup
                        mqttres = ToMqttMessage<BbrstinfoSuper>("state.reported.bbrstinfo", nestedObject);
                        break;
                    case "state.reported.batteryType":
                        mqttres = ToMqttMessage<BatteryType>("state.reported.batteryType", nestedObject);
                        break;
                    case "state.reported.tz":
                        mqttres = ToMqttMessage<TzSuper>("state.reported.tz", nestedObject/*, new TzConverter()*/);
                        break;
                    case "state.reported.cleanSchedule":
                        // Another rollup
                        mqttres = ToMqttMessage<CleanScheduleSuper>("state.reported.cleanSchedule", nestedObject);
                        break;
                    case "state.reported.bbchg":
                        // Another rollup
                        mqttres = ToMqttMessage<BbchgSuper>("state.reported.bbchg", nestedObject);
                        break;
                    case "state.reported.bbrun":
                        mqttres = ToMqttMessage<BbrunSuper>("state.reported.bbrun", nestedObject);
                        break;
                    case "state.reported.signal":
                        mqttres = ToMqttMessage<Signal>("state.reported.signal", nestedObject["signal"]);
                        break;
                    case "state.reported.mapUploadAllowed":
                        mqttres = ToMqttMessage<MapUploadAllowed>("state.reported.mapUploadAllowed", nestedObject);
                        break;
                    case "state.reported.pose":
                        mqttres = ToMqttMessage<Pose>("state.reported.pose", nestedObject);
                        break;
                    case "state.reported.audio":
                        break;
                    case "state.reported.lastCommand":
                        break;
                    case "state.reported.dock":
                        break;
                    case "state.reported.bbsys":
                        break;
                    default:
                        Console.WriteLine($"Unseen path type: {nestedPath}");
                        break;
                }

                //Console.WriteLine();
                return mqttres;
            });
            return await res;
        }

        private MqttMessage ToMqttMessage<T>(string Topic, JObject payload) {
            return new MqttMessage {
                Topic = Topic,
                Type = typeof(T),
                Raw = payload.ToString(Formatting.None),
                Payload = payload.ToObject<T>()
            };
        }

        private MqttMessage ToMqttMessage<T>(string Topic, JToken payload) {
            return new MqttMessage {
                Topic = Topic,
                Type = typeof(T),
                Raw = payload.ToString(Formatting.None),
                Payload = payload.ToObject<T>()
            };
        }

        /// <summary>
        /// Uses a custom deserialization class to take over for edge cases where the default will fail.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Topic"></param>
        /// <param name="payload"></param>
        /// <param name="customDeserialiser"></param>
        /// <returns></returns>
        private MqttMessage ToMqttMessage<T>(string Topic, JObject payload, JsonConverter customDeserialiser) {
            JsonSerializerSettings settings = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Objects,

            };
            settings.Converters.Add(customDeserialiser);
            return new MqttMessage {
                Topic = Topic,
                Type = typeof(T),
                Raw = payload.ToString(Formatting.None),
                Payload = JsonConvert.DeserializeObject<T>(payload.ToString(), settings)
            };
        }


        // Ignore all cert errors
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors) {
            return true;
        }

        /// <summary>
        /// Returns the password used to connect to the Roomba
        /// <para>
        /// This method will only work correctly if you have triggered wifi mode by holding the HOME button for several seconds until the roomba beeps.
        /// </para>
        /// </summary>
        /// <param name="RobotLocalIP"></param>
        /// <returns></returns>
        public string GetRoombaPassword(IPAddress RobotLocalIP) {

            TcpClient client = new TcpClient(RobotLocalIP.ToString(), port: 8883);
            Console.WriteLine("Connected to Roomba");

            // Create an SSL stream that will close the client's stream.
            SslStream sslStream = new SslStream(
                client.GetStream(),
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null
                );

            try {
                sslStream.AuthenticateAsClient("localhost");
            } catch(AuthenticationException e) {
                Console.WriteLine("Exception: {0}", e.Message);
                if(e.InnerException != null) {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                client.Close();
            }

            // Send message to Roomba to get password
            // TODO: figure out where this message was discovered, assuming it wasn't from
            // sniffing the traffic
            // Dug from https://github.com/pschmitt/roombapy/blob/master/roomba/password.py
            /*
                # this is 0xf0 (mqtt reserved) 0x05(data length)
                # 0xefcc3b2900 (data)
                [0]	240	byte // mqtt           0xf0
                [1]	5	byte // message length 0x05
                [2]	239	byte // message        0xef
                [3]	204	byte // message        0xcc
                [4]	59	byte // message        0x3b
                [5]	41	byte // message        0x29
                [6]	0	byte // message        0x00 - Based on errors returned, this seems like its a response flag, where 0x00 is OK, and 0x03 is ERROR? not sure
                                                      but details might be found in documentation for mqtt
             */
            byte[] messsage = { 0xf0, 0x05, 0xef, 0xcc, 0x3b, 0x29, 0x00 };
            sslStream.Write(messsage);
            sslStream.Flush();

            string roombaPassword = ReadMessage(sslStream);

            return roombaPassword;
        }

        private string ReadMessage(SslStream sslStream) {

            byte[] buffer = new byte[35];
            string resString = null;
            int bytes = -1;

            while(( bytes = sslStream.Read(buffer, 0, buffer.Length) ) > 0) {
                // First message from the vacuum the length of the password
                /*
                    [0]	240	byte // mqtt           0xf0
                    [1]	35	byte // message length 0x35
                    the message length includes the original 5 bytes we sent to it.
                 */
                if(bytes == 2) {
                    continue;
                } else if(bytes > 7) {

                    //https://github.com/pschmitt/roombapy/blob/master/roomba/password.py#L129
                    // mentions this in more detail, but I think I could simplify a lot of this.
                    // buffer could be 35, and just read all.

                    // Skip the first 5 bytes we sent previously (0xef, 0xcc, 0x3b, 0x29, 0x00)
                    // the remaining 30 bytes is our password
                    var finalBuffer = buffer.Skip(5).ToArray();
                    // The result is UTF-8
                    resString = Encoding.UTF8.GetString(finalBuffer);
                    break;
                } else {
                    // Here the response will be the first 4 bytes of the message we sent,
                    // followed by 0x03 to indicate an error? Not too sure on that
                    /*
                        [2]	239	byte // message        0xef
                        [3]	204	byte // message        0xcc
                        [4]	59	byte // message        0x3b
                        [5]	41	byte // message        0x29
                        [4]	3	byte // error byte?    0x03  - not sure how this maps yet
                     */
                    throw new Exception("Failed to retrieve password. Did you hold the home button until it beeped?");
                }
            }

            return resString;
        }

        private IMqttClient client;

        public async Task ConnectToRoombaViaMQTT(RoombaDetails roombaDetails) {
            var opts = new MqttClientOptions {
                ClientId = roombaDetails?.Username ?? "3144460891021710",
                ChannelOptions = new MqttClientTcpOptions {
                    Port = 8883,/*
                    TlsOptions = new MqttClientTlsOptions {
                        AllowUntrustedCertificates = true
                    },*/
                    TlsOptions = new MqttClientTlsOptions {
                        AllowUntrustedCertificates = true,
                        IgnoreCertificateChainErrors = true,
                        IgnoreCertificateRevocationErrors = true,
                        UseTls = true
                    },
                    Server = roombaDetails?.LocalIp.ToString() ?? "192.168.1.101"
                },
                Credentials = new MqttClientCredentials {
                    Username = roombaDetails?.Username ?? "3144460891021710",
                    Password = roombaDetails?.Password ?? ":1:1525667008:MJTo5KUdxVHInkWp"
                },
                CleanSession = false,
                ProtocolVersion = MQTTnet.Serializer.MqttProtocolVersion.V311
            };
            var factory = new MqttFactory();

            client = factory.CreateMqttClient();
            client.Connected += async (s, e) => {
                OnMessage(this, new RoombaReceivedMessageEvent {
                    Message = new MqttMessage {
                        Payload = "Connected",
                        Raw = "Connected",
                        Type = typeof(string),
                        Topic = "event.roomba.connected"
                    }
                });
                Console.WriteLine("Connected to Roomba");
                //await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());
            };

            client.Disconnected += (s, e) => {
                throw new Exception("discconected");
            };

            client.ApplicationMessageReceived += async (s, e) => {
                var resMessage = await ParseMQTTMessageToType(e.ApplicationMessage.Payload, e.ApplicationMessage.Topic);
                if(resMessage != null) {
                    OnMessage(this, new RoombaReceivedMessageEvent {
                        Message = resMessage
                    });
                    //Console.WriteLine($"Received message with topic {e.ApplicationMessage.Topic}: {resMessage.Raw}");
                } else {
                    Console.WriteLine($"Received message with topic {e.ApplicationMessage.Topic}: {{topic was not handled}}");
                    //throw new Exception($"Received message with topic {e.ApplicationMessage.Topic}: {{topic was not handled}}");
                }
            };

            try {
                await client.ConnectAsync(opts);
            } catch(Exception e) {
                Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + e);
            }
        }

        public async Task SendCommand(string commandString) {
            /*
            var cmd = new {
                command = new {
                    state = commandString
                },
                time = DateTime.Now.ToFileTimeUtc(),
                initiator = "localApp"
            };
            var cmdString = JsonConvert.SerializeObject(cmd);
            */
            /*
             
start: () => _apiCall('cmd', 'start'),
 function _apiCall (topic, command) {
    return new Promise((resolve, reject) => {
      let cmd = {command: command, time: Date.now() / 1000 | 0, initiator: 'localApp'};
      if (topic === 'delta') {
        cmd = {'state': command};
      }
      client.publish(topic, JSON.stringify(cmd), function (e) {
        if (e) return reject(e);
        resolve({ok: null}); // for retro compatibility
      });
    });
}

            {
                "command" : "start",
                time :
            }
             */
            DateTime foo = DateTime.UtcNow;
            long unixTime = ( (DateTimeOffset)foo ).ToUnixTimeSeconds();
            var applicationMessage = new MqttApplicationMessageBuilder()
                       .WithTopic("cmd")
                       .WithPayload($@"{{""command"":""{commandString}"",""time"":{unixTime},""initiator"":""localApp""}}")
                        .Build();
            try {
                await client.PublishAsync(applicationMessage);
            } catch(Exception e) {
                throw e;
            }
        }
    }
}
