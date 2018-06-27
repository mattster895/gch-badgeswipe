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
        public int worker_id_record = 0;

        public string worker_name { get; set; }
        public int worker_name_record = 1;

        public int worker_clearance { get; set; }
        public int worker_clearance_record = 2;

        public int workplace_id { get; set; }
        public int workplace_id_record = 3;

        public string workplace_name { get; set; }
        public int workplace_name_record = 4;

        public int secondary_workplace_id { get; set; }
        public int secondary_workplace_id_record = 5;

        public string secondary_workplace_name { get; set; }
        public int secondary_workplace_name_record = 6;

        public bool login_status { get; set; }
        public int login_status_record = 7;
    }
}
