using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPboxService
{
    public class StreamString
    {
        private readonly Stream _ioStream;
        private readonly UnicodeEncoding _sEncoding;

        public StreamString(Stream ioStream)
        {
            _ioStream = ioStream;
            _sEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;

            len = _ioStream.ReadByte() * 256;
            len += _ioStream.ReadByte();
            var inBuffer = new byte[len];
            _ioStream.Read(inBuffer, 0, len);

            return _sEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            var outBuffer = _sEncoding.GetBytes(outString);
            var len = outBuffer.Length;

            if (len > ushort.MaxValue)
                len = ushort.MaxValue;

            _ioStream.WriteByte((byte)(len / 256));
            _ioStream.WriteByte((byte)(len & 255));
            _ioStream.Write(outBuffer, 0, len);
            _ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}

