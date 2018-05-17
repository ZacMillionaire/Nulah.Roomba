using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nulah.Roomba {
    public class StaticHelpers {
        public static long GetAgreedTimestamp() {
            return ( (DateTimeOffset)DateTime.UtcNow ).ToUnixTimeSeconds();
        }
        /// <summary>
        /// A reasonably pointless method, but maybe I might need to change this later
        /// so this is a thing that now exists.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetUtcNow() {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a directory at the given location
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static bool CreateDirectory(string directory) {
            try {
                Directory.CreateDirectory(directory);
                return true;
            } catch {
                throw;
            }
        }

        /// <summary>
        /// Creates a file at the given location
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static bool CreateFile(string filename, string directory) {
            try {
                using(File.Create(Path.Combine(directory, filename))) {
                    return true;
                }
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
        private static bool CompareBytes(byte[] lhs, byte[] rhs) {
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

    }
}
