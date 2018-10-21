using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DT = System.Data;
using QC = System.Data.SqlClient;
using MyExtensions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace BadgeSwipeApp
{
    class SwipeManagerLaser
    {
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **SETUP**
        // These functions watch for swipes, and manage the steps that should be taken when a swipe happens
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void SwipeAgent(System.ComponentModel.BackgroundWorker worker, System.ComponentModel.DoWorkEventArgs e)
        {
            SwipeEntry currentSwipe = new SwipeEntry();
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
                    Console.WriteLine("Badge Swipe App is up and running");
                
                    while (!worker.CancellationPending)
                    {
                            while (GlobalVar.SwipeNumLaser > 0)
                            {
                                getEntry(connectionEntryDB, currentSwipe);
                                currentSwipe.debugPrint(GlobalVar.Debug);
                                SwipeProcess(connectionMainDB, currentSwipe);
                                worker.ReportProgress(0);
                                Thread.Sleep(100);
                            }
                    }                                    
                    connectionMainDB.Close();
                    connectionEntryDB.Close();
                }
        }

        public void SwipeProcess(QC.SqlConnection connection, SwipeEntry swipe)
        {
            bool processComplete = false; 

            // Save swiped ID details
            Workers swipeWorker = new Workers();
            swipeWorker.worker_id = swipe.sent_id;

            // Save swiped workplace details
            Workplaces newWorkplace = new Workplaces();
            newWorkplace.workplace_id = swipe.sent_workplace;

            // Fill ID and workplace shells
            FillWorker(connection, swipeWorker);
            FillWorkplace(connection, newWorkplace);

            // Get details of ID's current workplace 
            Workplaces oldWorkplace = new Workplaces();
            oldWorkplace.workplace_id = swipeWorker.workplace_id;
            FillWorkplace(connection, oldWorkplace);

            // Check if ID exists. If not, add to the workers database
            if (!check_worker_exist(connection, swipeWorker))
            {
                add_new_worker(connection, swipeWorker);
            }

            // first, check that this workplace accepts badge scans
            if (newWorkplace.workplace_badge)
            {
                // If the worker is authorized to log in/log out of cells
                if ((swipeWorker.worker_id != 0) && (swipeWorker.worker_clearance != 0))
                {
                    // Log out of old cells if necessary

                    if (swipeWorker.login_status && swipeWorker.workplace_id == 0)
                    {
                        change_login_status(connection, swipeWorker, false);
                    }

                    if (swipeWorker.login_status)
                    {

                        // Are they badging out?
                        if (query_logout(connection, swipeWorker, newWorkplace))
                        {

                            // set login to false

                            MakeFrame(false, swipeWorker, newWorkplace);
                            change_worker_workplace(connection, swipeWorker, false);
                            change_login_status(connection, swipeWorker, false);

                            // // if unique, set workplace worker entry to null
                            if (oldWorkplace.workplace_unique)
                            {
                                change_workplace_worker(connection, newWorkplace, false);
                            }

                            // // else, set workplace worker entry to first person still logged into this cell
                            else
                            {
                                // SQL to return where login = true and activeWorkplace.workplace_id = old/newWorkplace.workplace_id
                                change_workplace_worker(connection, newWorkplace, true);
                            }

                            if (worker_logged_in(connection, swipeWorker))
                            {
                                change_worker_workplace(connection, swipeWorker, true);
                                change_login_status(connection, swipeWorker, true);
                            }

                            processComplete = true;
                        }

                        // Is the worker's old cell exclusive,
                        // or the worker's new cell exclusive (?)
                        // log them out of the old cell
                        else if (oldWorkplace.workplace_exclusive || newWorkplace.workplace_exclusive)
                        {
                            change_login_status(connection, swipeWorker, false);
                            change_worker_workplace(connection, swipeWorker, false);
                            MakeFrame(false, swipeWorker, oldWorkplace);

                            if (oldWorkplace.workplace_unique)
                            {
                                change_workplace_worker(connection, oldWorkplace, false);
                            }
                            else
                            {
                                change_workplace_worker(connection, oldWorkplace, true);
                            }

                        }
                    }

                    // Log into a new cell
                    if (!processComplete)
                    {
                        // if(new cell is not empty and new cell is unique)
                        if ((newWorkplace.active_operator != 0) && (newWorkplace.workplace_unique))
                        {
                            Workers oldWorker = new Workers();
                            oldWorker.worker_id = newWorkplace.active_operator;
                            FillWorker(connection, oldWorker);

                            // log out old operator in new cell
                            change_login_status(connection, oldWorker, false);

                            // change workplace operator to NULL
                            change_workplace_worker(connection, newWorkplace, false);

                            // if the worker is logged in nowhere else,
                            if (!worker_logged_in(connection, oldWorker))
                            {
                                // change their workplace to NULL
                                change_worker_workplace(connection, oldWorker, false);
                            }
                            else
                            {
                                // change their workplace to the first place they're logged in
                                change_worker_workplace(connection, oldWorker, true);
                                change_login_status(connection, oldWorker, true);
                            }

                            // Send logout frame
                            MakeFrame(false, oldWorker, newWorkplace);
                        }

                        // log in new operator in new cell
                        {
                            change_login_status(connection, swipeWorker, true);
                            change_workplace_worker(connection, newWorkplace, swipeWorker);
                            change_worker_workplace(connection, swipeWorker, newWorkplace);
                            MakeFrame(true, swipeWorker, newWorkplace);
                        }
                    }
                }
            }
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **FRAME**
        // These functions generate and/or send the Sisteplant Captor Frames
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void MakeFrame(bool status, Workers worker, Workplaces workplace)
        {
            string Frame = "";
            char[] LaserStringTrim = { 'A', 'B', ' ' };
            // send in frame (single/double)
            if (status)
            {
                Frame = "INPW," + worker.worker_id + "," + workplace.workplace_name.Trim(LaserStringTrim);
                if (workplace.sibling_workplace_name != string.Empty && workplace.sibling_workplace_name != "" && !workplace.sibling_workplace_name.Contains("LASER"))
                {
                    Frame += "," + workplace.sibling_workplace_name.Trim();
                }
            }
            // send out frame (single/double)
            if (!status)
            {
                Frame = "OUTW," + worker.worker_id + "," + workplace.workplace_name.Trim(LaserStringTrim);
                if (workplace.sibling_workplace_name != string.Empty && workplace.sibling_workplace_name != "" && !workplace.sibling_workplace_name.Contains("LASER"))
                {
                    Frame += "," + workplace.sibling_workplace_name.Trim();
                }
            }
            if (GlobalVar.SendFrames)
            {
                send_frame(Frame);
            }
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
                    StreamWriter sw = new StreamWriter("SwipelogLaser.txt", true);
                    sw.WriteLine("(" + DateTime.Now.ToString("s") + "):\t" + frame);
                    sw.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                }
        }

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **UTILITY**
        // These functions are more all-purpose database manipulators
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void getEntry(QC.SqlConnection connection, SwipeEntry swipe)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT sent_workplace, sent_id
                FROM SwipeDataLasers" + 
                " WHERE entry_number = " + GlobalVar.StartSwipeLaser + ";";

                QC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    swipe.sent_workplace = reader.SafeGetInt(0);
                    swipe.sent_id = reader.SafeGetInt(1);
                }
                reader.Close();
        
            }
        }

        public void change_login_status(QC.SqlConnection connection, Workers worker, bool status)
        {
            // CHANGE IN DATABASE 
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                UPDATE Workers 
                SET login_status = '" + status + "'" +
                "WHERE worker_id = " + worker.worker_id + ";";
                command.ExecuteNonQuery();
            }
            worker.login_status = status;
        }

        public bool check_worker_exist(QC.SqlConnection connection, Workers worker)
        {
            int exist_id = 0;
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT * 
                FROM Workers
                WHERE worker_id = " + worker.worker_id + ";";
                QC.SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    exist_id = reader.SafeGetInt(0);
                }
                reader.Close();
                if (exist_id == 0)
                return false;
                else
                return true;
            }
        }

        public void add_new_worker(QC.SqlConnection connection, Workers worker)
        {
           // Add to the Worker Database
           // 0 - Unassigned
           // 1 - Staff
           // 2 - Staff2
           // 3 - Maintenance
           // 4 - Maint-AR
           // 5 - Tool and Die
           // 6 - Training

            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                    INSERT
                    INTO Workers (worker_id, worker_clearance, login_status)
                    VALUES (" + worker.worker_id + ", 0, 'FALSE');";
                command.ExecuteNonQuery();
            }

            EmailAgent emailAgent = new EmailAgent();
            emailAgent.MissingBadgeEmail(worker.worker_id);

            // Alert me, because this honestly should only be called in error

        }

        public bool query_logout(QC.SqlConnection connection, Workers worker, Workplaces newWorkplace)
        {

            int tempCheck = 0;

            // First, easy peasy. Is the current workers.workplace the same as the new one?
            if(worker.workplace_id == newWorkplace.workplace_id)
            {
                return true;
            }
            else if (worker.workplace_id == newWorkplace.sibling_workplace)
            {
                return true;
            }
            // Next, slightly less easy
            // using (var command = new QC.SqlCommand())
            //{
            //    command.Connection = connection;
            //    command.CommandType = DT.CommandType.Text;
            //    command.CommandText = "SELECT workplace_id FROM Workplaces WHERE active_operator = " + worker.worker_id + 
            //                          " AND (workplace_id = " + newWorkplace.workplace_id + " OR sibling_workplace = " + newWorkplace.workplace_id + ");";
            //    QC.SqlDataReader reader = command.ExecuteReader();
            //    while (reader.Read())
            //    {
            //        tempCheck = reader.SafeGetInt(0);
            //    }
            //    reader.Close();
            //}

            //if ((tempCheck == newWorkplace.workplace_id) || (tempCheck == newWorkplace.sibling_workplace))
            //    return true;

            else if(worker.worker_id == newWorkplace.active_operator)
            {
                return true;
            }

            else
                return false;
        }
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **WORKER FUNCTIONS**
        // These functions deal with the worker table in the database
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

            public bool worker_logged_in(QC.SqlConnection connection, Workers worker)
            {
            int check = 0;
            using (var command = new QC.SqlCommand())
                {
                
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT TOP 1 workplace_id
                FROM Workplaces
                WHERE active_operator = " + worker.worker_id + ";";
                QC.SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    check = reader.SafeGetInt(0);
                }
                reader.Close();
            }
            if (check != 0)
                return true;
            else
                return false;
            }

        public void change_worker_workplace(QC.SqlConnection connection, Workers worker, Workplaces workplace)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                UPDATE Workers
                SET workplace_id = " + workplace.workplace_id +
                ", workplace_name = '" + workplace.workplace_name +
                "' WHERE worker_id = " + worker.worker_id + ";";
                command.ExecuteNonQuery();
            }
            worker.workplace_id = workplace.workplace_id;
            worker.workplace_name = workplace.workplace_name;
        }

        public void change_worker_workplace(QC.SqlConnection connection, Workers worker, bool status)
        {
            if (!status)
            {
                // put null
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    UPDATE Workers
                    SET workplace_id = NULL,
                    workplace_name = NULL
                    WHERE worker_id = " + worker.worker_id + ";";
                    command.ExecuteNonQuery();
                    worker.workplace_id = 0;
                    worker.workplace_name = "";
                }
            }
            else
            {
                // update worker workplace details to show the first place they're logged in
                using (var command = new QC.SqlCommand())
                {
                    int tempID = 0;
                    string tempName = "";
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    SELECT TOP 1 workplace_id, workplace_name
                    FROM Workplaces
                    WHERE active_operator = " + worker.worker_id + ";";
                    QC.SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        tempID = reader.SafeGetInt(0);
                        Console.WriteLine(reader.SafeGetInt(0));
                        tempName = reader.SafeGetString(1);
                        Console.WriteLine(reader.SafeGetString(1));
                    }
                    reader.Close();
                    command.CommandText = @"
                    UPDATE Workers
                    SET workplace_id = " + tempID +
                    ",workplace_name = '" + tempName +
                    "' WHERE worker_id = " + worker.worker_id + ";";
                    command.ExecuteNonQuery();
                    worker.workplace_id = tempID;
                    worker.workplace_name = tempName;
                }
            }
        }




        public void FillWorker(QC.SqlConnection connection, Workers worker)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT * 
                FROM Workers
                WHERE worker_id = " + worker.worker_id + ";";
                QC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                            worker.worker_name = reader.SafeGetString(worker.worker_name_record);
                            worker.worker_clearance = reader.SafeGetInt(worker.worker_clearance_record);
                            worker.workplace_id = reader.SafeGetInt(worker.workplace_id_record);
                            worker.workplace_name = reader.SafeGetString(worker.workplace_name_record);
                            worker.login_status = reader.GetBoolean(worker.login_status_record);
                }
                reader.Close();
            }
        }
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **WORKPLACE FUNCTIONS**
        // These functions deal with the workplace table in the database
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        public void change_workplace_worker(QC.SqlConnection connection, Workplaces workplace, Workers worker)
        {
            using (var command = new QC.SqlCommand())
            {
            command.Connection = connection;
            command.CommandType = DT.CommandType.Text;
            command.CommandText = @"
            UPDATE Workplaces
            SET active_operator = " + worker.worker_id +
            " WHERE workplace_id = " + workplace.workplace_id +
            " OR workplace_id = " + workplace.sibling_workplace + ";";
            command.ExecuteNonQuery();
            workplace.active_operator = worker.worker_id;
            }        
        }

        public void change_workplace_worker(QC.SqlConnection connection, Workplaces workplace, bool type)
        {
            // Set to NULL
            if (!type)
            {
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    UPDATE Workplaces
                    SET active_operator = 0 " +
                    "WHERE workplace_id = " + workplace.workplace_id +
                    "OR workplace_id = " + workplace.sibling_workplace + ";";
                    command.ExecuteNonQuery();
                    workplace.active_operator = 0;
                }
            }

            // Set to first worker listed
            else
            {
                using (var command = new QC.SqlCommand())
                {
                    int tempID = 0;

                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;

                    command.CommandText = @"
                    SELECT TOP 1 worker_id
                    FROM Workers
                    WHERE workplace_id = " + workplace.workplace_id +
                    " OR workplace_id = " + workplace.sibling_workplace + ";";
                    QC.SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        tempID = reader.SafeGetInt(0);
                    }
                    reader.Close();

                    command.CommandText = @"
                    UPDATE Workplaces
                    SET active_operator = " + tempID +
                    " WHERE (workplace id = " + workplace.workplace_id +
                    " OR workplace_id = " + workplace.sibling_workplace + ");";
                    command.ExecuteNonQuery();

                    workplace.active_operator = tempID;
                }
            }
        }
        

        // This should probably be a method of the workplace model? I'm not sure. Look into this!
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
                    workplace.active_reference_version = reader.SafeGetInt(workplace.active_reference_version_record);
                    workplace.sibling_workplace = reader.SafeGetInt(workplace.sibling_workplace_record);
                    workplace.sibling_workplace_name = reader.SafeGetString(workplace.sibling_workplace_name_record);
                    workplace.workplace_unique = reader.GetBoolean(workplace.workplace_unique_record);
                    workplace.workplace_exclusive = reader.GetBoolean(workplace.workplace_exclusive_record);
                    workplace.workplace_badge = reader.GetBoolean(workplace.workplace_badge_record);
                    workplace.workplace_ref = reader.GetBoolean(workplace.workplace_ref_record);
                    workplace.workplace_program_specification = reader.SafeGetString(workplace.workplace_program_specification_record);
                }
                reader.Close();
            }
        }
    }
}
