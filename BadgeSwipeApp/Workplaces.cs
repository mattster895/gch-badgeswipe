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

        public int active_reference_version { get; set; }
        public int active_reference_version_record = 4;

        public int sibling_workplace { get; set; }
        public int sibling_workplace_record = 5;

        public string sibling_workplace_name { get; set; }
        public int sibling_workplace_name_record = 6;

        public bool workplace_unique { get; set; }
        public int workplace_unique_record = 7;

        public bool workplace_exclusive { get; set; }
        public int workplace_exclusive_record = 8;

        public bool workplace_badge { get; set; }
        public int workplace_badge_record = 9;

        public bool workplace_ref { get; set; }
        public int workplace_ref_record = 10;

        public string workplace_program_specification { get; set; }
        public int workplace_program_specification_record = 11;

        public void debugPrint(bool debug)
        {
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Debug Print Workplace Details");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Workplace ID - " + workplace_id);
                Console.WriteLine("Workplace Name - " + workplace_name);
                Console.WriteLine("Workplace Program Specification - " + workplace_program_specification);
                Console.WriteLine("Active Operator - " + active_operator);
                Console.WriteLine("Active Reference - " + active_reference);
                Console.WriteLine("Active Reference Version - " + active_reference_version);
                Console.WriteLine("Sibling Workplace ID - " + sibling_workplace);
                Console.WriteLine("Sibling Workplace Name - " + sibling_workplace_name);
                Console.WriteLine("Workplace Unique - " + workplace_unique);
                Console.WriteLine("Workplace Exclusive - " + workplace_exclusive);
                Console.WriteLine("Workplace Supports Badging - " + workplace_badge);
                Console.WriteLine("Workplace Supports Reference Scanning - " + workplace_ref);
            }
        }
        
    }
}
