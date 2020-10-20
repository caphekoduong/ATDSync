using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ATDSync.Models
{
    public class Attendance
    {
        public int id { get; set; }
        public string MaNV { get; set; }
        public DateTime tgQuet { get; set; }
        public bool isSync { get; set; }
    }
}
