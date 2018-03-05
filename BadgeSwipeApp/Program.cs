using System;
using System.ComponentModel;
using System.Threading;

namespace BadgeSwipeApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var worker1 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            var worker2 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            GlobalVar.StartValue = 0;
            GlobalVar.SwipeNum = 0;

            Console.WriteLine("Badge Swipe Appliction is up and running");

            worker1.DoWork += new DoWorkEventHandler(worker1_DoWork);
            worker1.ProgressChanged += new ProgressChangedEventHandler(worker1_ProgressChanged);
            worker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker1_RunWorkerCompleted);
            worker1.RunWorkerAsync();

            worker2.DoWork += new DoWorkEventHandler(worker2_DoWork);
            worker2.ProgressChanged += new ProgressChangedEventHandler(worker2_ProgressChanged);
            worker2.RunWorkerAsync();

            Console.WriteLine("Press any key to stop workers");
            Console.ReadKey();

            worker1.CancelAsync();
            worker2.CancelAsync();

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
            
            

        }

        private static void worker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            SwipeWatcher swipeWatcher = new SwipeWatcher();
            swipeWatcher.DependencyWatch(worker, e);

            
        }

        private static void worker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Get the Background Worker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;

            //Console.WriteLine(e.ProgressPercentage.ToString());
        }

        private static void worker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Console.WriteLine("Canceled!");
            }
            else if (e.Error != null)
            {
                Console.WriteLine("Error!");
            }
            else
            {
                Console.WriteLine("Swipe Watcher stopped");
            }
        }

        private static void worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            SwipeManager swipeManager = new SwipeManager();

            swipeManager.SwipeAgent(worker, e);
        }

        private static void worker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            GlobalVar.StartValue += 1;
            GlobalVar.SwipeNum =  GlobalVar.SwipeNum- 1;
            //Console.WriteLine("CHANGED " + GlobalVar.StartValue);
            //Console.WriteLine("CHANGED " + GlobalVar.SwipeNum);
            
        }


    }
}
