using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibVLCSharp.UWP.Sample
{
    public class MyStreamMediaInput : MediaInput
    {
        private readonly Stream _stream;
        private readonly byte[] _readBuffer = new byte[0x40000];
        private object sync = new object();

        /// <summary>
        /// Initializes a new instance of <see cref="StreamMediaInput"/>, which reads from the given .NET stream.
        /// </summary>
        /// <remarks>You are still responsible to dispose the stream you give as input.</remarks>
        /// <param name="stream">The stream to be read from.</param>
        public MyStreamMediaInput(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        /// LibVLC calls this method when it wants to open the media
        /// </summary>
        /// <param name="size">This value must be filled with the length of the media (or ulong.MaxValue if unknown)</param>
        /// <returns><c>true</c> if the stream opened successfully</returns>
        public override bool Open(out ulong size)
        {
            lock(sync)
            {

            try
            {
                try
                {
                    size = (ulong)_stream.Length;
                    Debug.WriteLine($">> Open.size {size}");
                }
                catch (Exception)
                {
                        Debug.WriteLine($">> Open.size exeption !!!!!!!!!!!!!");
                        // byte length of the bitstream or UINT64_MAX if unknown
                        size = ulong.MaxValue;
                    Debug.WriteLine($">> Open.size {size}");
                }

                if (_stream.CanSeek)
                {
                    Debug.WriteLine($">> Open seek 0");
                    _stream.Seek(0L, SeekOrigin.Begin);
                }

                return true;
            }
            catch (Exception)
            {
                size = 0UL;
                return false;
            }
            }
        }

        /// <summary>
        /// LibVLC calls this method when it wants to read the media
        /// </summary>
        /// <param name="buf">The buffer where read data must be written</param>
        /// <param name="len">The buffer length</param>
        /// <returns>The number of bytes actually read, -1 on error</returns>
        public override int Read(IntPtr buf, uint len)
        {
            lock (sync)
            { 
            try
            {
                int actualLen = Math.Min((int)len, _readBuffer.Length);

                var read = _stream.Read(_readBuffer, 0, Math.Min((int)len, _readBuffer.Length));
                Marshal.Copy(_readBuffer, 0, buf, read);
                //Debug.WriteLine($">> Read len {len}  actualLen {actualLen} bytesRead {read}");

                return read;
            }
            catch (Exception)
            {
                    Debug.WriteLine($">> Read exeption !!!!!!!!!!!!!");

                    return -1;
            }
        }
        }

        /// <summary>
        /// LibVLC calls this method when it wants to seek to a specific position in the media
        /// </summary>
        /// <param name="offset">The offset, in bytes, since the beginning of the stream</param>
        /// <returns><c>true</c> if the seek succeeded, false otherwise</returns>
        public override bool Seek(ulong offset)
        {
            lock(sync)
            {

            try
            {
                Debug.WriteLine($">> Seek offset {offset}");
                _stream.Seek((long)offset, SeekOrigin.Begin);
                return true;
            }
            catch (Exception)
            {
                    Debug.WriteLine($">> Seek exeption !!!!!!!!!!!!!");
                    return false;
            }
            }
        }

        /// <summary>
        /// LibVLC calls this method when it wants to close the media.
        /// </summary>
        public override void Close()
        {
            try
            {
                Debug.WriteLine($">> Close");
                if (_stream.CanSeek)
                    _stream.Seek(0, SeekOrigin.Begin);

            }
            catch (Exception)
            {
                Debug.WriteLine($">> Close exeption !!!!!!!!!!!!!");
                // ignored
            }
        }
    }
}
