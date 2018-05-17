using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nulah.Roomba.Models;
using Nulah.Roomba.Models.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nulah.Roomba {
    // Most of this code is roughly ported from GetRoombaPassword in https://github.com/koalazak/dorita980
    public class Roomba980 {

        private readonly string _poseRegex = @"({""theta"":[\d-]+,""point"":{[xy:\"",\d-]+}})";
        private readonly Logger _logger;

        public Roomba980() {
            _logger = new Logger();
            logFileName = $"Nulah.RoombaLogFile-{DateTime.UtcNow.ToString("s").Replace(":", "_") }.log";
        }

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

        private string logFileName;
        public string LogFileLocation = "./";

        public class RoombaReceivedMessageEvent : EventArgs {
            public MqttMessagePayload Message { get; set; }
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
        public List<string> Messages = new List<string>();

        private async Task<MqttMessagePayload> ParseMQTTMessageToPayload(byte[] byteArray, string topic) {
            var resString = Encoding.Default.GetString(byteArray);
            Task<MqttMessagePayload> res = Task.Run(() => {

                dynamic s = JsonConvert.DeserializeObject(resString);

                JObject nestedObject = s.state.reported;
                var nestedTopics = nestedObject.Children()
                    .Select(x => new {
                        Value = (JProperty)x,
                        Key = ( (JProperty)x ).Name,
                        Path = ( (JProperty)x ).Path,
                        ObjectNested = ( x.Children().Count() == 1 && x.First.Children().Count() > 0 ),
                        Type = Type.GetType($"Nulah.Roomba.Models.Responses.{( (JProperty)x ).Name}", false, true)
                    });

                var messageGroup = "[Grouped] " + string.Join(",", nestedTopics.Select(x => x.Key));

                DateTime timestamp = StaticHelpers.GetUtcNow();
                _logger.Append(resString, messageGroup);

                var parsedTopicsForLog = nestedTopics.Select(x => {

                    if(x.Key == "langs") {
                        JsonSerializerSettings settings = new JsonSerializerSettings {
                            TypeNameHandling = TypeNameHandling.Objects
                        };
                        settings.Converters.Add(new LangsConverter());

                        return new {
                            Topic = x.Key,
                            Path = ( x.ObjectNested ) ? x.Value.First.ToString(Formatting.None) : x.Value.ToString(Formatting.None),
                            obj = JsonConvert.DeserializeObject($"{{{x.Value.ToString()}}}", typeof(Langs), settings)
                        };
                    }
                    return new {
                        Topic = x.Key,
                        Path = ( x.ObjectNested ) ? x.Value.First.ToString(Formatting.None) : x.Value.ToString(Formatting.None),
                        obj = ( x.ObjectNested )
                        ? JsonConvert.DeserializeObject(x.Value.First.ToString(Formatting.None), x.Type)
                        : JsonConvert.DeserializeObject(x.Value.Parent.ToString(Formatting.None), x.Type)
                    };
                });
                /*
                List<string> messagesToLog = new List<string>();

                messagesToLog.Add(logMessage);
                */
                foreach(var ptfl in parsedTopicsForLog) {
                    _logger.Append(ptfl.Path, ptfl.Topic);
                    /*
                    logMessage = $"->\t{ptfl.Topic}\t{ptfl.Path}{Environment.NewLine}";
                    messagesToLog.Add(logMessage);
                    */
                }

                /*
                byte[] logBytes = Encoding.UTF8.GetBytes(string.Join("", messagesToLog));

                Task.Run(async () => {
                    using(var ss = File.Open(Path.Combine(LogFileLocation, logFileName), FileMode.OpenOrCreate)) {
                        ss.Seek(0, SeekOrigin.End);
                        await ss.WriteAsync(logBytes, 0, logBytes.Length);
                    }
                });
                */

                // Add to MqttMessage and figure out a way to bundle all the messages with it
                DateTime eventTime = DateTime.UtcNow;

                var nestedPath = nestedObject.First.Path;
                IEnumerable<MqttMessage> ms;
                if(nestedPath == "state.reported.pose") {
                    ms = Regex.Matches(resString, _poseRegex)
                        .Cast<Match>()
                        .Select(x => new MqttMessage {
                            Topic = "state.reported.pose",
                            Type = typeof(Pose),
                            Raw = x.Value,
                            Payload = JsonConvert.DeserializeObject<Pose>($"{x.Value}"),
                            TimeStamp = timestamp
                        });
                } else {
                    ms = nestedTopics.Select(x => {

                        if(x.Key == "langs") {
                            JsonSerializerSettings settings = new JsonSerializerSettings {
                                TypeNameHandling = TypeNameHandling.Objects
                            };
                            settings.Converters.Add(new LangsConverter());

                            return new MqttMessage {
                                Topic = $"state.reported.langs",
                                Raw = $"{{{x.Value.ToString()}}}",
                                Type = typeof(Langs),
                                Payload = JsonConvert.DeserializeObject($"{{{x.Value.ToString()}}}", typeof(Langs), settings),
                                TimeStamp = timestamp
                            };
                        }

                        return new MqttMessage {
                            Topic = $"state.reported.{x.Key}",
                            Type = x.Type,
                            Raw = ( x.ObjectNested ) ? x.Value.First.ToString(Formatting.None) : x.Value.ToString(Formatting.None),
                            Payload = ( x.ObjectNested )
                                ? JsonConvert.DeserializeObject(x.Value.First.ToString(Formatting.None), x.Type)
                                : JsonConvert.DeserializeObject(x.Value.Parent.ToString(Formatting.None), x.Type),
                            TimeStamp = timestamp
                        };
                    });
                }

                MqttMessagePayload mqttres = new MqttMessagePayload {
                    Messages = ms.ToArray()
                };
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
                ClientId = roombaDetails.Username,
                ChannelOptions = new MqttClientTcpOptions {
                    Port = 8883,
                    TlsOptions = new MqttClientTlsOptions {
                        AllowUntrustedCertificates = true,
                        IgnoreCertificateChainErrors = true,
                        IgnoreCertificateRevocationErrors = true,
                        UseTls = true
                    },
                    Server = roombaDetails.LocalIp.ToString()
                },
                Credentials = new MqttClientCredentials {
                    Username = roombaDetails.Username,
                    Password = roombaDetails.Password
                },
                CleanSession = false,
                ProtocolVersion = MQTTnet.Serializer.MqttProtocolVersion.V311,
                CommunicationTimeout = TimeSpan.FromSeconds(30)
            };

            var factory = new MqttFactory();

            client = factory.CreateMqttClient();

            client.Connected += async (s, e) => {
                OnMessage(this, new RoombaReceivedMessageEvent {
                    Message = new MqttMessagePayload {
                        Messages = new[]{
                            new MqttMessage {
                                Payload = "Connected",
                                Raw = "Connected",
                                Type = typeof(string),
                                Topic = "event.roomba.connected"
                            }
                        }
                    }
                });
                Console.WriteLine("Connected to Roomba");
                //await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());
            };

            client.Disconnected += async (s, e) => {
                Console.WriteLine("Disconnected. Reconnecting");
                await client.ConnectAsync(opts);
                //throw new Exception("discconected");
            };

            client.ApplicationMessageReceived += async (s, e) => {
                var resMessage = await ParseMQTTMessageToPayload(e.ApplicationMessage.Payload, e.ApplicationMessage.Topic);
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
