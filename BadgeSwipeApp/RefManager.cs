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

                        currentRef.debugPrint(GlobalVar.Debug);

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
            bool repair = false;

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
                    // Check for "Repair Job"
                    if (repair)
                    {
                        // Do something if Repair Job exists
                        process_complete = true;
                    }
                        
                        
                    // Otherwise, continue as normal
                }

                if (!process_complete)
                {
                    // Log out current reference
                    if (oldRef.reference_number != 0)
                    {

                        change_ref_log(connection, oldRef, scanWorkplace, false);
                        
                    }

                    // Log in new reference
                    if(scanRef.reference_number != 0)
                    {

                        change_ref_log(connection, scanRef, scanWorkplace, true);

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
            char[] LaserStringTrim = { 'A', 'B', ' ' }; // good

            // Start with a blank frame
            string Frame = "";

            // send in frame
            if (status)
            {
                Frame = "INPM," + Ref.manufacturing_reference.Trim() + "," + Workplace.workplace_name.Trim(LaserStringTrim);
                if (Workplace.workplace_name.Trim().EndsWith("A"))
                {
                    Frame = Frame + ",SIDE A";
                }
                if (Workplace.workplace_name.Trim().EndsWith("B"))
                {
                    Frame = Frame + ",SIDE B";
                }
            }
            if (!status)
            {
                Frame = "OUTM," + Ref.manufacturing_reference.Trim() + "," + Workplace.workplace_name.Trim(LaserStringTrim);
                if (Workplace.workplace_name.Trim().EndsWith("A"))
                {
                    Frame = Frame + ",SIDE A";
                }
                if (Workplace.workplace_name.Trim().EndsWith("B"))
                {
                    Frame = Frame + ",SIDE B";
                }
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
                Workplace.active_reference = Ref.reference_number;
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
                    reference.part_number = reader.SafeGetString(reference.part_number_record);
                    reference.manufacturing_reference = reader.SafeGetString(reference.manufacturing_reference_record);
                    reference.program_specification = reader.SafeGetString(reference.program_specification_record);
                    reference.cycle_time = reader.SafeGetInt(reference.cycle_time_record);
                    reference.parts_produced = reader.SafeGetInt(reference.parts_produced_record);
                    reference.child_reference = reader.SafeGetInt(reference.child_reference_record);
                }
                reader.Close();
            }

            //Console.WriteLine();
            //Console.WriteLine("DEBUG");
            //Console.WriteLine("----------------------");
            //Console.WriteLine("Reference Number = " + reference.reference_number);
            //Console.WriteLine("Part Number = " + reference.part_number);
            //Console.WriteLine("Manufacturing Reference = " + reference.manufacturing_reference);
            //Console.WriteLine("Program Specification = " + reference.program_specification);
            //Console.WriteLine("Cycle Time = " + reference.cycle_time);
            //Console.WriteLine("Parts Produced = " + reference.parts_produced);
            //Console.WriteLine("Workplace ID = " + reference.workplace_id);
            //Console.WriteLine("Workplace Name = " + reference.workplace_name);
            //Console.WriteLine("Login Status = " + reference.login_status);
            
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
                    workplace.workplace_name = reader.SafeGetString(workplace.workplace_name_record);
                    workplace.active_operator = reader.SafeGetInt(workplace.active_operator_record);
                    workplace.active_reference = reader.SafeGetInt(workplace.active_reference_record);
                    workplace.sibling_workplace = reader.SafeGetInt(workplace.sibling_workplace_record);
                    workplace.sibling_workplace_name = reader.SafeGetString(workplace.sibling_workplace_name_record);
                    workplace.workplace_unique = reader.GetBoolean(workplace.workplace_unique_record);
                    workplace.workplace_exclusive = reader.GetBoolean(workplace.workplace_exclusive_record);
                }
                reader.Close();
            }
        }

        public void change_ref_log(QC.SqlConnection connection, Refs refs, Workplaces workplace, bool status)
        {
            if (status)
            {
                int tempHold;
                
                MakeFrame(true, refs, workplace);
                if (refs.child_reference != 0)
                {
                    tempHold = refs.reference_number;
                    refs.reference_number = refs.child_reference;
                    FillReference(connection, refs);
                    change_ref_log(connection, refs, workplace, true);
                    refs.reference_number = tempHold;
                    FillReference(connection, refs);
                }
            }
        
            if (!status)
            {
                
                MakeFrame(false, refs, workplace);
                if (refs.child_reference != 0)
                {
                    refs.reference_number = refs.child_reference;
                    FillReference(connection, refs);
                    change_ref_log(connection, refs, workplace, false);
                }
            }
        }
    }
}
