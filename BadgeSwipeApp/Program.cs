using System;
using System.ComponentModel;
using System.Threading;

namespace BadgeSwipeApp
{
    class Program
    {
        static void Main(string[] args)
        {



            GlobalVar.Debug = false;
            GlobalVar.SwipeAgent = false;
            GlobalVar.RefAgent = false;
            GlobalVar.SendFrames = false;

            launchMenu();


            GlobalVar.StartSwipe = 0;
            GlobalVar.SwipeNum = 0;
            GlobalVar.StartRef = 0;
            GlobalVar.RefNum = 0;
            var worker1 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            var worker2 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            var worker3 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            var worker4 = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            if (GlobalVar.SwipeAgent)
            {
                worker1.DoWork += new DoWorkEventHandler(worker1_DoWork);
                worker1.ProgressChanged += new ProgressChangedEventHandler(worker1_ProgressChanged);
                worker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker1_RunWorkerCompleted);
                worker1.RunWorkerAsync();

                worker2.DoWork += new DoWorkEventHandler(worker2_DoWork);
                worker2.ProgressChanged += new ProgressChangedEventHandler(worker2_ProgressChanged);
                worker2.RunWorkerAsync();
            }

            if (GlobalVar.RefAgent)
            {
                worker3.DoWork += new DoWorkEventHandler(worker3_DoWork);
                worker3.ProgressChanged += new ProgressChangedEventHandler(worker3_ProgressChanged);
                worker3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker3_RunWorkerCompleted);
                worker3.RunWorkerAsync();

                worker4.DoWork += new DoWorkEventHandler(worker4_DoWork);
                worker4.ProgressChanged += new ProgressChangedEventHandler(worker4_ProgressChanged);
                worker4.RunWorkerAsync();
            }

            Console.WriteLine("Press any key to stop the current operation");
            Console.ReadKey();

        
            worker1.CancelAsync();
            worker2.CancelAsync();
            worker3.CancelAsync();
            worker4.CancelAsync();

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
            
        }

        static string boolFormatter(bool check)
        {
            if (check)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                return "Enabled";
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                return "Disabled";
            }
        }

        static void launchMenu()
        {
            bool menuComplete = false;
            

            while (!menuComplete)
            {
                Console.WriteLine("Welcome to Captor Frame App for GCH");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Debug Mode: " + boolFormatter(GlobalVar.Debug));
                Console.ResetColor();
                Console.WriteLine("411 Badge Scanners: " + boolFormatter(GlobalVar.SwipeAgent));
                Console.ResetColor();
                Console.WriteLine("416 Badge Scanners: " + boolFormatter(GlobalVar.SwipeAgent));
                Console.ResetColor();
                Console.WriteLine("Laser Badge Scanners: " + boolFormatter(GlobalVar.SwipeAgent));
                Console.ResetColor();
                Console.WriteLine("Ref Scan Agent: " + boolFormatter(GlobalVar.RefAgent));
                Console.ResetColor();
                Console.WriteLine("Send Frames: " + boolFormatter(GlobalVar.SendFrames));
                Console.ResetColor();
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("0) Run with these settings");
                Console.WriteLine("1) Change Debug Mode");
                Console.WriteLine("2) Change 411 Badge Swipe Agent");
                Console.WriteLine("3) Change 416 Badge Swipe Agent");
                Console.WriteLine("4) Change Laser Badge Swipe Agent");
                Console.WriteLine("5) Change Ref Scan Agent");
                Console.WriteLine("6) Change Send Frames");
                Console.WriteLine("7) Refresh Laser Workplaces");
                Console.WriteLine();
                char selection = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (selection)
                {
                    case '0':
                        menuComplete = true;
                        Console.Clear();
                        break;
                    case '1':
                        if (GlobalVar.Debug)
                            GlobalVar.Debug = false;
                        else
                            GlobalVar.Debug = true;
                        Console.Clear();
                        break;
                    case '2':
                        if (GlobalVar.SwipeAgent)
                            GlobalVar.SwipeAgent = false;
                        else
                            GlobalVar.SwipeAgent = true;
                        Console.Clear();
                        break;
                    case '3':
                        if (GlobalVar.SwipeAgent)
                            GlobalVar.SwipeAgent = false;
                        else
                            GlobalVar.SwipeAgent = true;
                        Console.Clear();
                        break;
                    case '4':
                        if (GlobalVar.SwipeAgent)
                            GlobalVar.SwipeAgent = false;
                        else
                            GlobalVar.SwipeAgent = true;
                        Console.Clear();
                        break;
                    case '5':
                        if (GlobalVar.RefAgent)
                            GlobalVar.RefAgent = false;
                        else
                            GlobalVar.RefAgent = true;
                        Console.Clear();
                        break;
                    case '6':
                        if (GlobalVar.SendFrames)
                            GlobalVar.SendFrames = false;
                        else
                            GlobalVar.SendFrames = true;
                        Console.Clear();
                        break;
                    case '7':
                        Console.Clear();
                        RefreshFromFile refresh = new RefreshFromFile();
                        refresh.RefreshLaserWorkplace();
                        Console.WriteLine("Laser Workplaces refreshed.");
                        break;
                    default:
                        Console.Clear();
                        Console.WriteLine("Please make a valid selection.");
                        break;

                }
            }
        }

        private static void worker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            SwipeWatcher swipeWatcher = new SwipeWatcher();
            swipeWatcher.DependencyWatch(worker, e, "BadgeSwipe-411");   
        }

        private static void worker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Get the Background Worker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
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
        }

        private static void worker3_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            RefWatcher refWatcher = new RefWatcher();
            refWatcher.DependencyWatch(worker, e);
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
