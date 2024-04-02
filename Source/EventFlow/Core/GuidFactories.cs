// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
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
using System.Threading;

namespace EventFlow.Core
{
    public static class GuidFactories
    {
        public static class Comb
        {
            private static int _counter;

            private static long GetTicks()
            {
                var i = Interlocked.Increment(ref _counter);
                return DateTime.UtcNow.Ticks + i;
            }

            /// <summary>
            /// Generates a GUID values that causes less index fragmentation when stored
            /// in e.g. <c>uniqueidentifier</c> columns in MSSQL.
            /// </summary>
            /// <example>
            /// 2825c1d8-4587-cc55-08c1-08d6bde2765b
            /// 901337ba-c64b-c6d4-08c2-08d6bde2765b
            /// 45d57ba2-acc5-ce80-08c3-08d6bde2765b
            /// 36528acf-352a-c28c-08c4-08d6bde2765b
            /// 6fc88b5e-3782-c8fd-08c5-08d6bde2765b
            /// </example>
            public static Guid Create()
            {
                var uid = Guid.NewGuid().ToByteArray();
                var binDate = BitConverter.GetBytes(GetTicks());

                return new Guid(
                    new[]
                        {
                            uid[0], uid[1], uid[2], uid[3],
                            uid[4], uid[5],
                            uid[6], (byte)(0xc0 | (0xf & uid[7])),
                            binDate[1], binDate[0],
                            binDate[7], binDate[6], binDate[5], binDate[4], binDate[3], binDate[2]
                        });
            }

            /// <summary>
            /// Generates a GUID values that causes less index fragmentation when stored
            /// in e.g. <c>nvarchar(n)</c> columns in MSSQL.
            /// </summary>
            /// <example>
            /// 899ee1b9-bde2-08d6-20d8-b7e20375c7c9
            /// 899f09b9-bde2-08d6-fd1c-5ec8f3349bcf
            /// 899f09ba-bde2-08d6-1521-51d781607ac4
            /// 899f09bb-bde2-08d6-7e6a-fe84f5237dc4
            /// 899f09bc-bde2-08d6-c2f0-276123e06fcf
            /// </example>
            public static Guid CreateForString()
            {
                /*
                    From: https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray 
                    Note that the order of bytes in the returned byte array is different from the string
                    representation of a Guid value. The order of the beginning four-byte group and the
                    next two two-byte groups is reversed, whereas the order of the last two-byte group
                    and the closing six-byte group is the same.
                */

                var uid = Guid.NewGuid().ToByteArray();
                var binDate = BitConverter.GetBytes(GetTicks());

                return new Guid(
                    new[]
                        {
                            binDate[0], binDate[1], binDate[2], binDate[3],
                            binDate[4], binDate[5],
                            binDate[6], binDate[7],
                            uid[0], uid[1],
                            uid[2], uid[3], uid[4], uid[5], uid[6], (byte)(0xc0 | (0xf & uid[7])),
                        });
            }
        }

        /// <summary>
        /// Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
        /// http://code.logos.com/blog/2011/04/generating_a_deterministic_guid.html
        /// </summary>
        public static class Deterministic
        {
            // It is safe to not clean up since span-based TryComputeHash rents an array and releases it every time
            private static readonly SHA1 Algorithm = SHA1.Create();

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
                var charSpan = name.AsSpan();
                Span<byte> byteSpan = stackalloc byte[Encoding.UTF8.GetByteCount(charSpan)];
                var sizeOf = Encoding.UTF8.GetBytes(charSpan, byteSpan);

                return Create(namespaceId, byteSpan.Slice(0, sizeOf));
            }

            public static Guid Create(Guid namespaceId, byte[] nameBytes)
            {
                return Create(namespaceId, nameBytes.AsSpan());
            }

            public static Guid Create(Guid namespaceId, Span<byte> nameSpan)
            {
                // Always use version 5 (version 3 is MD5, version 5 is SHA1)
                const int version = 5;

                if (namespaceId == default) throw new ArgumentNullException(nameof(namespaceId));
                if (nameSpan.Length == 0) throw new ArgumentNullException(nameof(nameSpan));

                // Convert the namespace UUID to network order (step 3)
                Span<byte> namespaceSpan = stackalloc byte[16]; // Guid length is 16.
                if (!namespaceId.TryWriteBytes(namespaceSpan))
                {
                    throw new ApplicationException("Failed to copy namespaceId to a span"); // Should never happen
                }
                SwapByteOrder(namespaceSpan);

                // Compute the hash of the name space ID concatenated with the name (step 4)
                Span<byte> hash = stackalloc byte[20]; // SHA1 produces 160-bit hash.
                Span<byte> combinedSpan = stackalloc byte[namespaceSpan.Length + nameSpan.Length];
                namespaceSpan.CopyTo(combinedSpan);
                nameSpan.CopyTo(combinedSpan.Slice(namespaceSpan.Length));

                lock (Algorithm) // We have to lock a shared instance since it is stateful
                {
                    if (!Algorithm.TryComputeHash(combinedSpan, hash, out _))
                    {
                        throw new ApplicationException("Failed to compute hash"); // Should never happen
                    }
                }

                // Most bytes from the hash are copied straight to the bytes of the new
                // GUID (steps 5-7, 9, 11-12)
                Span<byte> newGuid = stackalloc byte[16];
                hash.Slice(0, 16).CopyTo(newGuid);

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

            internal static void SwapByteOrder(Span<byte> guid)
            {
                SwapBytes(guid, 0, 3);
                SwapBytes(guid, 1, 2);
                SwapBytes(guid, 4, 5);
                SwapBytes(guid, 6, 7);
            }

            internal static void SwapBytes(Span<byte> guid, int left, int right)
            {
                var temp = guid[left];
                guid[left] = guid[right];
                guid[right] = temp;
            }
        }
    }
}
