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
    class RefWatcher
    {
        public void DependencyWatch(
            System.ComponentModel.BackgroundWorker worker,
            System.ComponentModel.DoWorkEventArgs e)
        {
            var connectionString = "Server = 192.168.176.133; " +
                                    "Database=Badge_Swipe_EntryDB;" +
                                    "Trusted_Connection=yes";
            var tableDependency = new SqlTableDependency<RefEntry>(connectionString, "RefData");

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

        static void TableDependency_Changed(object sender, RecordChangedEventArgs<RefEntry> e)
        {

            if (e.ChangeType == ChangeType.Insert)
            {
                var changedEntity = e.Entity;
                if (GlobalVar.StartRef == 0)
                {
                    GlobalVar.StartRef = changedEntity.entry_number;
                }
                GlobalVar.RefNum += 1;

            }
        }

        static void TableDependency_OnError(object sender, ErrorEventArgs e)
        {
            Exception ex = e.Error;
            throw ex;
        }
    }
}
