using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace Nulah.Roomba {
    public class Logger {

        private const string DEFAULT_LOG_LOCATION = "./logs/";
        private const string DEFAULT_SEPERATOR = "\t";
        private const string DEFAULT_MESSAGE_GROUP = "UNGROUPED";

        // By default log to the executing directory.
        private string _logLocation;
        // Automatically generated
        private string _logName;
        // Unless overriden via a constructor, the log file is tab seperated in an attempt
        // to infuriate someone who would disagree with this format (its because the log file can contain commas)
        private string _seperator;
        // Default group to log as if none given
        private string _generalMessageGroup;

        public Logger() {
            InitLogger();
        }

        public Logger(string logFileLocation = DEFAULT_LOG_LOCATION, string seperator = DEFAULT_SEPERATOR, string messageGroup = DEFAULT_MESSAGE_GROUP) {
            InitLogger(logFileLocation, seperator, messageGroup);
        }

        private void InitLogger(string logFileLocation = DEFAULT_LOG_LOCATION, string seperator = DEFAULT_SEPERATOR, string defaultLogGroup = DEFAULT_MESSAGE_GROUP) {
            _logLocation = logFileLocation;
            _seperator = seperator;
            _generalMessageGroup = defaultLogGroup;
            _logName = $"Nulah.RoombaLogFile-{DateTime.UtcNow.ToString("s").Replace(":", "_") }.log";

            TestDirectory();
            var fileTest = TestCanWrite();
            if(fileTest == false) {
                throw new Exception($"Unable to successfully write to log file. Either the file could not be created or opened, the write failed, or the content written did not match what was expected.");
            }
        }

        private bool TestDirectory() {
            try {
                Directory.CreateDirectory(_logLocation);
                if(Directory.Exists(_logLocation) == false) {
                    throw new DirectoryNotFoundException($"Unable to locate directory '{_logLocation}'");
                }

                return true;
            } catch {
                throw;
            }
        }

        private bool TestCanWrite() {
            try {
                return Task.Run(async () => {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(await WriteToFileAsync("--- Logger init start ---.", timestamp: 0));
                    sb.Append(await WriteToFileAsync("Directory test passed.", timestamp: 1));
                    sb.Append(await WriteToFileAsync("Running file write async test.", timestamp: 2));
                    sb.Append(await WriteToFileAsync("File write passed. Ironically if it failed you wouldn't see this at all.", timestamp: 3));
                    sb.Append(await WriteToFileAsync("Attempting to read back contents of file...", timestamp: 4));

                    byte[] existingFileBytes = ReadLogFileContents(sb.Length);
                    var contentMatches = CompareBytes(Encoding.UTF8.GetBytes(sb.ToString()), existingFileBytes);

                    if(contentMatches) {
                        await WriteToFileAsync("...File read/write test passed. Async method working as expected. Ready for logging.", timestamp: 5);
                        await WriteToFileAsync("--- Logger init end ---.", timestamp: 6);
                        return contentMatches;
                    }
                    return false;
                }).Result;
            } catch {
                throw;
            }
        }

        private byte[] ReadLogFileContents(int contentLength) {
            try {
                byte[] content = new byte[contentLength];
                using(var ss = File.Open(Path.Combine(_logLocation, _logName), FileMode.Open)) {
                    int bytesRead = 0;
                    do {
                        bytesRead += ss.Read(content, bytesRead, contentLength);
                    } while(bytesRead != contentLength);
                }
                return content;
            } catch {
                throw;
            }
        }

        /// <summary>
        /// Returns whether or not 2 byte arrays are equal.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        private bool CompareBytes(byte[] lhs, byte[] rhs) {
            if(lhs.Length != rhs.Length) {
                return false;
            }

            for(var i = 0; i < lhs.Length; i++) {
                if(lhs[i] != rhs[i]) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Blocking
        /// <para>
        /// Writes the given string to the log file
        /// </para>
        /// </summary>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        public string WriteToFile(string logMessage, string logGroup = null, long timestamp = long.MinValue) {

            if(string.IsNullOrWhiteSpace(logMessage)) {
                throw new ArgumentNullException("Argument logMessage cannot be null or an empty string.");
            }

            if(logGroup == null) {
                logGroup = _generalMessageGroup;
            }

            // The timestamp can be overridden, and 0 is used as the default for init events.
            // If the long passed is less than 0, default it to the current timestamp.
            // Maybe I might want to log negative values in the future? (lol)
            if(timestamp < 0) {
                timestamp = StaticHelpers.GetAgreedTimestamp();
            }

            string logLine = CreateLogLine(logMessage, logGroup, timestamp);
            byte[] logBytes = Encoding.UTF8.GetBytes(logLine);

            try {
                using(var ss = File.Open(Path.Combine(_logLocation, _logName), FileMode.OpenOrCreate)) {
                    ss.Seek(0, SeekOrigin.End);
                    ss.Write(logBytes, 0, logBytes.Length);
                }
                return logLine;
            } catch {
                throw;
            }
        }

        /// <summary>
        /// Async
        /// <para>
        /// Writes the given string to the log file asynchronously
        /// </para>
        /// </summary>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        public async Task<string> WriteToFileAsync(string logMessage, string logGroup = null, long timestamp = long.MinValue) {

            if(string.IsNullOrWhiteSpace(logMessage)) {
                throw new ArgumentNullException("Argument logMessage cannot be null or an empty string.");
            }

            if(logGroup == null) {
                logGroup = _generalMessageGroup;
            }

            // The timestamp can be overridden, and 0 is used as the default for init events.
            // If the long passed is less than 0, default it to the current timestamp.
            // Maybe I might want to log negative values in the future? (lol)
            if(timestamp < 0) {
                timestamp = StaticHelpers.GetAgreedTimestamp();
            }

            string logLine = CreateLogLine(logMessage, logGroup, timestamp);
            byte[] logBytes = Encoding.UTF8.GetBytes(logLine);

            try {
                using(var ss = File.Open(Path.Combine(_logLocation, _logName), FileMode.OpenOrCreate)) {
                    ss.Seek(0, SeekOrigin.End);
                    await ss.WriteAsync(logBytes, 0, logBytes.Length).ConfigureAwait(false);
                }
                return logLine;
            } catch {
                throw;
            }
        }

        public string CreateLogLine(string logMessage, long timestamp) {
            return CreateLogLine(logMessage, _generalMessageGroup, timestamp);
        }

        private string CreateLogLine(string logMessage, string logGroup, long timestamp) {
            List<string> parts = new List<string>();
            parts.Add(timestamp.ToString());
            if(string.IsNullOrWhiteSpace(logGroup)) {
                parts.Add(_generalMessageGroup);
            } else {
                parts.Add(logGroup);
            }
            parts.Add(logMessage);
            parts.Add(Environment.NewLine);
            return string.Join(_seperator, parts);
        }
    }
}
