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

        public bool login_status { get; set; }
        public int login_status_record = 5;

        public void debugPrint(bool debug)
        {
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Debug Print Worker Details");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Worker ID - " + worker_id);
                Console.WriteLine("Worker Name - " + worker_name);
                Console.WriteLine("Worker Clearance - " + worker_clearance);
                Console.WriteLine("Workplace ID - " + workplace_id);
                Console.WriteLine("Workplace Name - " + workplace_name);
                Console.WriteLine("Login Status - " + login_status);
            }
        }
    }
}
