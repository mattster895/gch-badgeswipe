using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.SqlClient;

namespace BadgeSwipeApp
{
    class SwipeWatcher
    {
        static int watchSwitch = 0;

        public void DependencyWatch(
            System.ComponentModel.BackgroundWorker worker,
            System.ComponentModel.DoWorkEventArgs e,
            string progSpec)
        {
            

            if (progSpec.Equals("411"))
            {
                watchSwitch = 1;
            }
            if (progSpec.Equals("416"))
            {
                watchSwitch = 2;
            }
            if (progSpec.Equals("Lasers"))
            {
                watchSwitch = 3;
            }

            var connectionString = "Server = 192.168.176.133; " +
                                  "Database=Badge_Swipe_EntryDB;" +
                                  "Trusted_Connection=yes";
            var tableDependency = new SqlTableDependency<SwipeEntry>(connectionString, "SwipeData-"+progSpec);

            
            using (tableDependency)
            {
                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.OnError += TableDependency_OnError;
                tableDependency.Start();
                
                while (!worker.CancellationPending)
                {

                }

                tableDependency.Stop();
            }
        }

        static void TableDependency_Changed(object sender, RecordChangedEventArgs<SwipeEntry> e)
        {

            if (e.ChangeType == ChangeType.Insert)
            {
                var changedEntity = e.Entity;
                switch (watchSwitch)
                {
                    case 1:
                        
                        if (GlobalVar.StartSwipe411 == 0)
                        {
                            GlobalVar.StartSwipe411 = changedEntity.entry_number;
                        }
                        GlobalVar.SwipeNum411 += 1;
                        break;
                    case 2:
                        if (GlobalVar.StartSwipe416 == 0)
                        {
                            GlobalVar.StartSwipe416 = changedEntity.entry_number;
                        }
                        GlobalVar.SwipeNum416 += 1;
                        break;
                    case 3:
                        if (GlobalVar.StartSwipeLaser == 0)
                        {
                            GlobalVar.StartSwipeLaser = changedEntity.entry_number;
                        }
                        GlobalVar.SwipeNumLaser += 1;
                        break;
                    default:
                        break;

                }
            }
        }

        static void TableDependency_OnError(object sender, ErrorEventArgs e)
        {
            Exception ex = e.Error;
            throw ex;
        }
    }
}
