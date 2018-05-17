using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nulah.Roomba.Models;
using System.Linq;

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


        private Queue<LogMessage> _logQueue;
        private IObservable<long> _logQueueObserver;

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

            InitLogRx();

            TestDirectory();
            TestFile();

            StartLogSubscription();

            Append("Logger ready", timestamp: 0);
        }

        private void InitLogRx() {
            _logQueue = new Queue<LogMessage>();
            _logQueueObserver = Observable.Interval(TimeSpan.FromSeconds(1));
        }

        private bool TestDirectory() {
            if(StaticHelpers.CreateDirectory(_logLocation)) {
                Append("Created Directory", timestamp: 0);
                return true;
            }
            return false;
        }

        private bool TestFile() {
            if(StaticHelpers.CreateFile(_logName, _logLocation)) {
                Append("Created Log file", timestamp: 0);
                return true;
            }
            return false;
        }

        private void StartLogSubscription() {
            _logQueueObserver.Subscribe(x => {
                if(_logQueue.Count > 0) {
                    switch(_logQueue.Count) {
                        case 1:
                            WriteToFile(_logQueue.Dequeue());
                            break;
                        default:
                            WriteToFile(_logQueue.ToArray());
                            _logQueue.Clear();
                            break;
                    }
                }
            });
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

        private readonly object fileLock = new object();

        /// <summary>
        /// Blocking
        /// <para>
        /// Writes the given string to the log file
        /// </para>
        /// </summary>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        private string WriteToFile(LogMessage logMessage) {
            lock(fileLock) {
                string logLine = CreateLogLine(logMessage.Message, logMessage.Group, logMessage.Timestamp);
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
        }

        /// <summary>
        /// Blocking
        /// <para>
        /// Writes the given string to the log file
        /// </para>
        /// </summary>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        private string WriteToFile(LogMessage[] logMessages) {
            lock(fileLock) {
                string[] logLines = logMessages.Select(x => CreateLogLine(x.Message, x.Group, x.Timestamp)).ToArray();
                string logBatch = string.Join(null, logLines);
                byte[] logBytes = Encoding.UTF8.GetBytes(logBatch);

                try {
                    using(var ss = File.Open(Path.Combine(_logLocation, _logName), FileMode.OpenOrCreate)) {
                        ss.Seek(0, SeekOrigin.End);
                        ss.Write(logBytes, 0, logBytes.Length);
                    }
                    return logBatch;
                } catch {
                    throw;
                }
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

        /// <summary>
        /// Adds a new log entry
        /// </summary>
        /// <param name="logMessage"></param>
        /// <param name="logGroup"></param>
        /// <param name="timestamp"></param>
        public void Append(string logMessage, string logGroup = null, long timestamp = long.MinValue) {
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

            _logQueue.Enqueue(new LogMessage {
                Message = logMessage,
                Timestamp = timestamp,
                Group = logGroup
            });
        }
    }
}
