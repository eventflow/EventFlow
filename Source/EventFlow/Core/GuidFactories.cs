// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Security.Cryptography;
using System.Text;

namespace EventFlow.Core
{
    public static class GuidFactories
    {
        /// <summary>
        /// Creates a sequential Guid that can be used to avoid database fragmentation
        /// http://stackoverflow.com/a/2187898
        /// </summary>
        public static class Comb
        {
            public static Guid Create()
            {
                var destinationArray = Guid.NewGuid().ToByteArray();
                var time = new DateTime(0x76c, 1, 1);
                var now = DateTime.Now;
                var span = new TimeSpan(now.Ticks - time.Ticks);
                var timeOfDay = now.TimeOfDay;
                var bytes = BitConverter.GetBytes(span.Days);
                var array = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));

                Array.Reverse(bytes);
                Array.Reverse(array);
                Array.Copy(bytes, bytes.Length - 2, destinationArray, destinationArray.Length - 6, 2);
                Array.Copy(array, array.Length - 4, destinationArray, destinationArray.Length - 4, 4);

                return new Guid(destinationArray);
            }
        }

        /// <summary>
        /// Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
        /// http://code.logos.com/blog/2011/04/generating_a_deterministic_guid.html
        /// </summary>
        public static class Deterministic
        {
            public static class Namespaces
            {
                public static readonly Guid Events = Guid.Parse("387F5B61-9E98-439A-BFF1-15AD0EA91EA0");
                public static readonly Guid Commands = Guid.Parse("4286D89F-7F92-430B-8E00-E468FE3C3F59");
            }

            // Modified from original
            // https://github.com/LogosBible/Logos.Utility/blob/master/src/Logos.Utility/GuidUtility.cs
            //
            // Copyright 2007-2013 Logos Bible Software
            // 
            // Permission is hereby granted, free of charge, to any person obtaining a copy of
            // this software and associated documentation files(the "Software"), to deal in
            // the Software without restriction, including without limitation the rights to
            // use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
            // of the Software, and to permit persons to whom the Software is furnished to do
            // so, subject to the following conditions:
            // 
            // The above copyright notice and this permission notice shall be included in all
            // copies or substantial portions of the Software.
            // 
            // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
            // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
            // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
            // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
            // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
            // SOFTWARE.

            public static Guid Create(Guid namespaceId, string name)
            {
                if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

                // Convert the name to a sequence of octets (as defined by the standard or conventions of its namespace) (step 3)
                // ASSUME: UTF-8 encoding is always appropriate
                var nameBytes = Encoding.UTF8.GetBytes(name);

                return Create(namespaceId, nameBytes);
            }

            public static Guid Create(Guid namespaceId, byte[] nameBytes)
            {
                // Always use version 5 (version 3 is MD5, version 5 is SHA1)
                const int version = 5;

                if (namespaceId == default(Guid)) throw new ArgumentNullException(nameof(namespaceId));
                if (nameBytes.Length == 0) throw new ArgumentNullException(nameof(nameBytes));

                // Convert the namespace UUID to network order (step 3)
                var namespaceBytes = namespaceId.ToByteArray();
                SwapByteOrder(namespaceBytes);

                // Comput the hash of the name space ID concatenated with the name (step 4)
                byte[] hash;
                using (var algorithm = SHA1.Create())
                {
                    algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
                    algorithm.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
                    hash = algorithm.Hash;
                }

                // Most bytes from the hash are copied straight to the bytes of the new
                // GUID (steps 5-7, 9, 11-12)
                var newGuid = new byte[16];
                Array.Copy(hash, 0, newGuid, 0, 16);

                // Set the four most significant bits (bits 12 through 15) of the time_hi_and_version
                // field to the appropriate 4-bit version number from Section 4.1.3 (step 8)
                newGuid[6] = (byte)((newGuid[6] & 0x0F) | (version << 4));

                // Set the two most significant bits (bits 6 and 7) of the clock_seq_hi_and_reserved
                // to zero and one, respectively (step 10)
                newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

                // Convert the resulting UUID to local byte order (step 13)
                SwapByteOrder(newGuid);
                return new Guid(newGuid);
            }

            internal static void SwapByteOrder(byte[] guid)
            {
                SwapBytes(guid, 0, 3);
                SwapBytes(guid, 1, 2);
                SwapBytes(guid, 4, 5);
                SwapBytes(guid, 6, 7);
            }

            internal static void SwapBytes(byte[] guid, int left, int right)
            {
                var temp = guid[left];
                guid[left] = guid[right];
                guid[right] = temp;
            }
        }
    }
}
