using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPboxService
{
    public class ReadMessageSent
    {
        private readonly string _data;
        private readonly StreamString _ss;

        public ReadMessageSent(StreamString str, string data)
        {
            _data = data;
            _ss = str;
        }

        public void Start()
        {
            _ss.WriteString(_data);
        }
    }
}
