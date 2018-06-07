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
        public string workplace_name { get; set; }
        public int active_operator { get; set; }
        public string active_operator_name { get; set; }
        public int active_operator_clearance { get; set; }
        public string active_reference { get; set; }
        public int sibling_workplace { get; set; }
        public string sibling_workplace_name { get; set; }
        public bool workplace_unique { get; set; }
        public bool workplace_exclusive { get; set; }
        
    }
}
