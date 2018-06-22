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
                        Console.WriteLine("Start Ref - " + GlobalVar.StartRef);
                        Console.WriteLine("RefNum - " + GlobalVar.RefNum);

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

            // Save scanned reference details
            References scannedRef = new References();
            scannedRef.reference_number = reference.sent_ref;

            // Save workplace scan details
            Workplaces scanWorkplace = new Workplaces();
            scanWorkplace.workplace_id = reference.sent_workplace;

            // Fill reference and workplace shells
            FillReference(connection, scannedRef);
            FillWorkplace(connection, scanWorkplace);

            // If new scanned reference != current reference
            if (scanWorkplace.active_reference != scannedRef.reference_number)
            {
                //Save details of reference already in that workplace
                References oldRef = new References();
                oldRef.reference_number = scanWorkplace.active_reference;
                FillReference(connection, oldRef);

                // Log out current reference
                change_login_status(connection, oldRef, false);
                MakeFrame(false, scannedRef.part_number, scannedRef.workplace_name);
                
                // Log in new reference
                change_login_status(connection, scannedRef, true);
                MakeFrame(true, scannedRef.part_number, scannedRef.workplace_name);
            }


            // Else, do nothing

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

        public void change_login_status(QC.SqlConnection connection, References reference, bool status)
        {
            // CHANGE IN DATABASE 
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                UPDATE References 
                SET login_status = '" + status + "'" +
                "WHERE reference_number = " + reference.reference_number + ";";
                command.ExecuteNonQuery();
                //Console.WriteLine("Change Log Status - Executed");
            }
        }

        public void FillReference(QC.SqlConnection connection, References reference)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT *
                FROM References
                WHERE reference_id = " + reference.reference_number + ";";
                QC.SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    reference.part_number = reader.SafeGetString(1);
                    reference.program_specification = reader.SafeGetString(2);
                    reference.cycle_time = reader.SafeGetInt(3);
                    reference.parts_produced = reader.SafeGetInt(4);
                    reference.workplace_id = reader.SafeGetInt(5);
                    reference.workplace_name = reader.SafeGetString(6);
                    reference.login_status = reader.GetBoolean(7);
                }
                reader.Close();
            }
        }

        public void FillWorkplace(QC.SqlConnection connection, Workplaces workplace)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT * 
                FROM Workplaces
                WHERE workplace_id = " + workplace.workplace_id + ";";
                QC.SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    workplace.workplace_name = reader.SafeGetString(1);
                    workplace.active_operator = reader.SafeGetInt(2);
                    workplace.active_operator_name = reader.SafeGetString(3);
                    workplace.active_operator_clearance = reader.SafeGetInt(4);
                    workplace.active_reference = reader.SafeGetInt(5);
                    workplace.sibling_workplace = reader.SafeGetInt(6);
                    workplace.sibling_workplace_name = reader.SafeGetString(7);
                    workplace.workplace_unique = reader.GetBoolean(8);
                    workplace.workplace_exclusive = reader.GetBoolean(9);
                }
                reader.Close();
            }
        }

    }
}
