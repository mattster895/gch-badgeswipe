﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    class Refs
    {
        public int reference_number { get; set; }
        public int reference_record = 0;

        public string part_number { get; set; }
        public int part_number_record = 1;

        public string manufacturing_reference { get; set; }
        public int manufacturing_reference_record = 2;

        public string program_specification { get; set; }
        public int program_specification_record = 3;

        public int cycle_time { get; set; }
        public int cycle_time_record = 4;

        public int parts_produced { get; set; }
        public int parts_produced_record = 5;

        public int workplace_id { get; set; }
        public int workplace_id_record = 6;

        public string workplace_name { get; set; }
        public int workplace_name_record = 7;

        public bool login_status { get; set; }
        public int login_status_record = 8;
    }
}
