using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FileInfoFTP
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }
        public FileInfoFTP(byte[] data, string name)
        {
            this.Data = data;
            this.Name = name;
        }
    }
}
