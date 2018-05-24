using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    class Workers
    {
        public int worker_id { get; set; }
        public string worker_name { get; set; }
        public int worker_clearance { get; set; }
        public int workplace_id { get; set; }
        public string workplace_name { get; set; }
        public int secondary_workplace_id { get; set; }
        public string secondary_workplace_name { get; set; }
        public bool login_status { get; set; }
    }
}
