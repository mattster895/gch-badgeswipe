using System;
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

        public int order_version { get; set; }
        public int order_version_record = 3;

        public string program_specification { get; set; }
        public int program_specification_record = 4;

        public int cycle_time { get; set; }
        public int cycle_time_record = 5;

        public int parts_produced { get; set; }
        public int parts_produced_record = 6;

        public int child_reference { get; set; }
        public int child_reference_record = 7;

        public void debugPrint(bool debug)
        {
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Debug Print Ref Details");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Reference Number - " + reference_number);
                Console.WriteLine("Part Number - " + part_number);
                Console.WriteLine("Manufacturing Reference - " + manufacturing_reference);
                Console.WriteLine("Order Version - " + order_version);
                Console.WriteLine("Program Specification - " + program_specification);
                Console.WriteLine("Cycle Time - " + cycle_time);
                Console.WriteLine("Parts Produced - " + parts_produced);
                Console.WriteLine("Child Reference - " + child_reference);
            }       
        }

    }
}
