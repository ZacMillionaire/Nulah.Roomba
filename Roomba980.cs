using Newtonsoft.Json;
using Nulah.Roomba.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
                // First message from the vacuum is [ 240 (0xf0 - mqtt reserved), 35 (message to follow length) ]
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
                    throw new Exception("Failed to retrieve password. Did you hold the home button until it beeped?");
                }
            }

            return resString;
        }
    }
}
