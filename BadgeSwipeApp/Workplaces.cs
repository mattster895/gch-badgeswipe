using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    class Workplaces
    {
        public int workplace_id { get; set; }
        public int workplace_id_record = 0;

        public string workplace_name { get; set; }
        public int workplace_name_record = 1;

        public int active_operator { get; set; }
        public int active_operator_record = 2;

        public string active_operator_name { get; set; }
        public int active_operator_name_record = 3;

        public int active_operator_clearance { get; set; }
        public int active_operator_clearance_record = 4;

        public int active_reference { get; set; }
        public int active_reference_record = 5;

        public int sibling_workplace { get; set; }
        public int sibling_workplace_record = 6;

        public string sibling_workplace_name { get; set; }
        public int sibling_workplace_name_record = 7;

        public bool workplace_unique { get; set; }
        public int workplace_unique_record = 8;

        public bool workplace_exclusive { get; set; }
        public int workplace_exclusive_record = 9;
        
    }
}
