using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    class AltRefs
    {
        public int entry_number { get; set; }
        public int entry_number_record = 0;

        public int standard_ref { get; set; }
        public int sent_workplace_record = 1;

        public int alt_version { get; set; }
        public int sent_ref_record = 2;

        public bool alt_ref { get; set; }
        public int alt_ref_record = 3;

        public bool ghost_ref { get; set; }
        public int ghost_ref_record = 4;

        public string alt_order { get; set; }
        public int alt_order_record = 5;

        public void debugPrint(bool debug)
        {
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Debug Print AltRef Details");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Entry Number - " + entry_number);
                Console.WriteLine("Standard Ref - " + standard_ref);
                Console.WriteLine("Alternate Version - " + alt_version);
                Console.WriteLine("Alternate Ref? - " + alt_ref);
                Console.WriteLine("Ghost Ref? - " + ghost_ref);
                Console.WriteLine("Alternate Order String - " + alt_order);
            }
        }
    }
}
