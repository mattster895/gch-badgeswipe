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
            RefEntry currentRef = new RefEntry();
            using (var connectionMainDB = new QC.SqlConnection(
                "Server = 192.168.176.133; " +
                "Database=Badge_Swipe_MainDB;" +
                "Trusted_Connection=yes;"))
                using (var connectionEntryDB = new QC.SqlConnection(
                    "Server = 192.168.176.133; " +
                    "Database=Badge_Swipe_EntryDB;" +
                    "Trusted_Connection=yes;"))
            {
                connectionMainDB.Open();
                connectionEntryDB.Open();
                Console.WriteLine("Reference Scan App is up and running");
                
                while (!worker.CancellationPending)
                {
                    while (GlobalVar.RefNum > 0)
                    {
                        getEntry(connectionEntryDB, currentRef);

                        Console.WriteLine("Writing Frame for Last Ref");
                        Console.WriteLine("Ref - " + currentRef.sent_ref);
                        Console.WriteLine("Workplace - " + currentRef.sent_workplace);
                        Console.WriteLine("Timestamp - " + currentRef.timestamp);
                
                        //Console.WriteLine("Start Ref - " + GlobalVar.StartRef);
                        //Console.WriteLine("RefNum - " + GlobalVar.RefNum);

                        RefProcess(connectionMainDB, currentRef);
                        worker.ReportProgress(0);
                        Thread.Sleep(100);
                    }
                }
                connectionMainDB.Close();
                connectionEntryDB.Close();
            }
        }

        public void RefProcess(QC.SqlConnection connection, RefEntry refEntry)
        {
            bool process_complete = false;
            bool reaper = false;

            // Save scanned reference details
            Refs scanRef = new Refs();
            scanRef.reference_number = refEntry.sent_ref;

            // Save workplace scan details
            Workplaces scanWorkplace = new Workplaces();
            scanWorkplace.workplace_id = refEntry.sent_workplace;

            // Fill reference and workplace shells
            FillReference(connection, scanRef);
            FillWorkplace(connection, scanWorkplace);

            
            // If new scanned reference != current reference
            if (scanWorkplace.active_reference != scanRef.reference_number)
            {
                //Save details of reference already in that workplace
                Refs oldRef = new Refs();
                oldRef.reference_number = scanWorkplace.active_reference;
                FillReference(connection, oldRef);

                if (scanRef.reference_number == 0)
                {
                    // Check for "Reaper Job"
                    if (reaper)
                    {
                        // Do something if Reaper Job exists
                        process_complete = true;
                    }
                        
                        
                    // Otherwise, continue as normal
                }

                if (!process_complete)
                {
                    // Log out current reference
                    if (oldRef.reference_number != 0)
                    {
                        change_login_status(connection, oldRef, false);
                        MakeFrame(false, oldRef, scanWorkplace);
                    }

                    // Log in new reference
                    if(scanRef.reference_number != 0)
                    {
                        change_login_status(connection, scanRef, true);
                        MakeFrame(true, scanRef, scanWorkplace);
                    }
                    change_workplace_reference(connection, scanRef, scanWorkplace);

                }
            }


            // Else, do nothing

        }

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **FRAME**
        // These functions generate and/or send the Sisteplant Captor Frames
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void MakeFrame(bool status, Refs Ref, Workplaces Workplace)
        {
            // Input Reference : INPM,Reference,Workplace,Headstock1 | Headstock2 |…..| HeadstockN
            // Output Reference: OUTM,Reference,Workplace,Headstock1 | Headstock2 |…..| HeadstockN


            // In the SQL database, dual sided lasers are labeled as LASERX A and LASERX B, this trimset cuts it down to the Captor LASERX name
            char[] LaserStringTrim = { 'A', 'B', ' ' };

            // Start with a blank frame
            string Frame = "";

            // send in frame
            if (status)
            {
                Frame = "INPM," + Ref.part_number.Trim() + "," + Workplace.workplace_name.Trim(LaserStringTrim);
                
                // Headstock?
            }
            if (!status)
            {
                Frame = "OUTM," + Ref.part_number.Trim() + "," + Workplace.workplace_name.Trim(LaserStringTrim);

                // Headstock?
            }

            //send_frame(Frame);
            write_frame(Frame);
                
        }

        public void send_frame(string frame)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress captorAddress = IPAddress.Parse("192.168.176.134");
            IPEndPoint endPoint = new IPEndPoint(captorAddress, 1038);
            byte[] send_buffer = Encoding.UTF8.GetBytes(frame);
            sock.SendTo(send_buffer, endPoint);
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
        public void getEntry(QC.SqlConnection connection, RefEntry reference)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
        SELECT sent_workplace, sent_ref, timestamp
        FROM RefData
        WHERE entry_number = " + GlobalVar.StartRef + ";";

                QC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    reference.sent_workplace = reader.SafeGetInt(0);
                    reference.sent_ref = reader.SafeGetInt(1);
                    reference.timestamp = reader.GetDateTime(2);
                }
                reader.Close();

            }
        }

        public void change_login_status(QC.SqlConnection connection, Refs reference, bool status)
        {
            // CHANGE IN DATABASE 
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                UPDATE Refs 
                SET login_status = '" + status + "'" +
                "WHERE reference_number = " + reference.reference_number + ";";
                command.ExecuteNonQuery();
                //Console.WriteLine("Change Log Status - Executed");
            }
        }

        public void change_workplace_reference(QC.SqlConnection connection, Refs Ref, Workplaces Workplace)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                UPDATE Workplaces
                SET active_reference = " + Ref.reference_number +
                "WHERE workplace_id = " + Workplace.workplace_id + ";";
                command.ExecuteNonQuery();
            }
        }

        public void FillReference(QC.SqlConnection connection, Refs reference)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT *
                FROM Refs
                WHERE reference_number = " + reference.reference_number + ";";
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

            Console.WriteLine();
            Console.WriteLine("DEBUG");
            Console.WriteLine("----------------------");
            Console.WriteLine("Reference Number = " + reference.reference_number);
            Console.WriteLine("Part Number = " + reference.part_number);
            Console.WriteLine("Program Specification = " + reference.program_specification);
            Console.WriteLine("Cycle Time = " + reference.cycle_time);
            Console.WriteLine("Parts Produced = " + reference.parts_produced);
            Console.WriteLine("Workplace ID = " + reference.workplace_id);
            Console.WriteLine("Workplace Name = " + reference.workplace_name);
            Console.WriteLine("Login Status = " + reference.login_status);
            
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
