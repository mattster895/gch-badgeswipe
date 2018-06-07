using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DT = System.Data;
using QC = System.Data.SqlClient;
using SQLReaderExtensions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace BadgeSwipeApp
{
    class RefManager
    {
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **SETUP**
        // These functions watch for swipes, and manage the steps that should be taken when a swipe happens
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void RefAgent(System.ComponentModel.BackgroundWorker worker, System.ComponentModel.DoWorkEventArgs e)
        {
            RefData currentRef = new RefData();
            using (var connectionA = new QC.SqlConnection(
                "Server = 192.168.176.133; " +
                "Database=Badge_Swipe_MainDB;" +
                "Trusted_Connection=yes;"))
                using (var connectionB = new QC.SqlConnection(
                    "Server = 192.168.176.133; " +
                    "Database=Badge_Swipe_EntryDB;" +
                    "Trusted_Connection=yes;"))
            {
                connectionA.Open();
                connectionB.Open();
                Console.WriteLine("Go");
                while (!worker.CancellationPending)
                {
                    while (GlobalVar.SwipeNum > 0)
                    {
                        getEntry(connectionB, currentRef);

                        Console.WriteLine("Writing Frame for Last Ref");
                        Console.WriteLine("Ref - " + currentRef.sent_ref);
                        Console.WriteLine("Workplace - " + currentRef.sent_workplace);

                        RefProcess(connectionA, currentRef);
                        worker.ReportProgress(0);
                        Thread.Sleep(100);
                    }
                }
                connectionA.Close();
                connectionB.Close();
            }
        }

        public void RefProcess(QC.SqlConnection connection, RefData reference)
        {
            bool processComplete = false;

            // Set up shells for database data

            // Fill reference shell

            

        }

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **FRAME**
        // These functions generate and/or send the Sisteplant Captor Frames
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void MakeFrame(bool status, string part_data, string workplace_name)
        {

        }

        public void send_frame(string frame)
        {

        }
        public void write_frame(string frame)
        {
            try
            {
                StreamWriter sw = new StreamWriter("Reflog.txt", true);
                sw.WriteLine("(" + DateTime.Now.ToString("s") + "):\t" + frame);
                sw.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **UTILITY**
        // These functions are more all-purpose database manipulators
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        public void getEntry(QC.SqlConnection connection, RefData reference)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
        SELECT sent_workplace, sent_ref
        FROM RefData
        WHERE entry_number = " + GlobalVar.StartRef + ";";

                QC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    reference.sent_workplace = reader.SafeGetInt(0);
                    reference.sent_ref = reader.SafeGetInt(1);
                }
                reader.Close();

            }
        }

    }
}
