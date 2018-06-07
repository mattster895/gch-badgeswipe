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

            GlobalVar.StartSwipe = 0;
            GlobalVar.SwipeNum = 0;

            var worker3 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            var worker4 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            GlobalVar.StartRef = 0;
            GlobalVar.RefNum = 0;

            Console.WriteLine("Badge Swipe Appliction is up and running");

            worker1.DoWork += new DoWorkEventHandler(worker1_DoWork);
            worker1.ProgressChanged += new ProgressChangedEventHandler(worker1_ProgressChanged);
            worker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker1_RunWorkerCompleted);
            worker1.RunWorkerAsync();

            worker2.DoWork += new DoWorkEventHandler(worker2_DoWork);
            worker2.ProgressChanged += new ProgressChangedEventHandler(worker2_ProgressChanged);
            worker2.RunWorkerAsync();

            Console.WriteLine("Reference Scan Application is up and running");
            worker3.DoWork += new DoWorkEventHandler(worker3_DoWork);
            worker3.ProgressChanged += new ProgressChangedEventHandler(worker3_ProgressChanged);
            worker3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker3_RunWorkerCompleted);
            worker3.RunWorkerAsync();

            worker4.DoWork += new DoWorkEventHandler(worker4_DoWork);
            worker4.ProgressChanged += new ProgressChangedEventHandler(worker4_ProgressChanged);
            worker4.RunWorkerAsync();

            Console.WriteLine("Press any key to stop workers");
            Console.ReadKey();

            worker1.CancelAsync();
            worker2.CancelAsync();
            worker3.CancelAsync();
            worker4.CancelAsync();

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

            GlobalVar.StartSwipe += 1;
            GlobalVar.SwipeNum =  GlobalVar.SwipeNum- 1;
            //Console.WriteLine("CHANGED " + GlobalVar.StartSwipe);
            //Console.WriteLine("CHANGED " + GlobalVar.SwipeNum);
            
        }

        private static void worker3_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            RefWatcher refWatcher = new RefWatcher();
            refWatcher.DependencyWatch(worker);
        }

        private static void worker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Get the Background Worker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
        }

        private static void worker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Console.WriteLine("Cancelled");
            }
            else if (e.Error != null)
            {
                Console.WriteLine("Error");
            }
            else
            {
                Console.WriteLine("Reference Watcher stopped");
            }
        }

        private static void worker4_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            RefManager refManager = new RefManager();

            refManager.RefAgent(worker, e);
        }

        private static void worker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            GlobalVar.StartRef += 1;
            GlobalVar.RefNum = GlobalVar.RefNum - 1;
        }



    }
}
