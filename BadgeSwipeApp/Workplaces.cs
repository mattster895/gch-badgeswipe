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

        public int active_reference { get; set; }
        public int active_reference_record = 3;

        public int sibling_workplace { get; set; }
        public int sibling_workplace_record = 4;

        public string sibling_workplace_name { get; set; }
        public int sibling_workplace_name_record = 5;

        public bool workplace_unique { get; set; }
        public int workplace_unique_record = 6;

        public bool workplace_exclusive { get; set; }
        public int workplace_exclusive_record = 7;

        public void debugPrint(bool debug)
        {
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Debug Print Workplace Details");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Workplace ID - " + workplace_id);
                Console.WriteLine("Workplace Name - " + workplace_name);
                Console.WriteLine("Active Operator - " + active_operator);
                Console.WriteLine("Active Reference - " + active_reference);
                Console.WriteLine("Sibling Workplace ID - " + sibling_workplace);
                Console.WriteLine("Sibling Workplace Name - " + sibling_workplace_name);
                Console.WriteLine("Workplace Unique - " + workplace_unique);
                Console.WriteLine("Workplace Exclusive - " + workplace_exclusive);
            }
        }
        
    }
}
