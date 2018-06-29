using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    class RefEntry
    {
        public int entry_number { get; set; }
        public int entry_number_record = 0;

        public int sent_workplace { get; set; }
        public int sent_workplace_record = 1;

        public int sent_ref { get; set; }
        public int sent_ref_record = 2;

        public DateTime timestamp { get; set; }
        public int timestamp_record = 3;

        public void debugPrint(bool debug)
        {
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Debug Print Ref Entry Details");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Entry Number - " + entry_number);
                Console.WriteLine("Sent Workplace - " + sent_workplace);
                Console.WriteLine("Sent Ref - " + sent_ref);
                Console.WriteLine("Timestamp - " + timestamp);
            }
        }
    }
}
