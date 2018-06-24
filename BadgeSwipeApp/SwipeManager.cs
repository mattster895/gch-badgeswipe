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
    class SwipeManager
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
                while(GlobalVar.SwipeNum>0)
                {
                    getEntry(connectionEntryDB, currentSwipe);

                        
                        
                        Console.WriteLine("Writing Frame for Last Swipe");
                        Console.WriteLine("ID - " + currentSwipe.sent_id);
                        Console.WriteLine("Workplace - " + currentSwipe.sent_workplace);
                        Console.WriteLine("Timestamp - " + currentSwipe.timestamp);
                        //Console.WriteLine("Start Swipe - " + GlobalVar.StartSwipe);
                        //Console.WriteLine("SwipeNum - " + GlobalVar.SwipeNum);
                        
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
            if (!CheckExist(connection, swipeWorker))
            {
                //Console.WriteLine("CREATE WORKER ID");
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    INSERT
                    INTO Workers (worker_id, worker_clearance, login_status)
                    VALUES (" + swipeWorker.worker_id + ", 1, 'FALSE');";
                    command.ExecuteNonQuery();
                }
            }
            // Check if Workplace exists. If not, bad scan, dump it.
                // TO BE IMPLEMENTED
            
            // What are we doing? Logging in or logging out?

            // If worker exists
            if (swipeWorker.worker_id != 0)
            {
                // Logout of old workplace if necessary
                if (swipeWorker.login_status)
                {
                    if (oldWorkplace.workplace_exclusive)
                    {
                        change_login_status(connection, swipeWorker.worker_id, false);
                        MakeFrame(false, swipeWorker.worker_id, oldWorkplace.workplace_name, oldWorkplace.sibling_workplace_name);
                        if(oldWorkplace.workplace_id == newWorkplace.workplace_id)
                        {
                            processComplete = true;
                        }
                    }
                    else if (newWorkplace.active_operator == swipeWorker.worker_id)
                    {
                        change_workplace_worker(connection,newWorkplace.workplace_id,newWorkplace.sibling_workplace,0,swipeWorker.worker_id);
                        MakeFrame(false, swipeWorker.worker_id, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name);
                        
                        // If worker is not logged in anywhere else, set login to FALSE
                        if(!CheckForWorkerLoggedIn(connection, swipeWorker.worker_id))
                        {
                            change_login_status(connection, swipeWorker.worker_id, false);
                        }
                        processComplete = true;
                    }
                }

                
                if (!processComplete)
                {
                    if(newWorkplace.workplace_unique && !CheckNull(connection,newWorkplace.workplace_id))
                    {
                        // log out current worker
                        change_workplace_worker(connection, newWorkplace.workplace_id, newWorkplace.sibling_workplace, swipeWorker.worker_id, newWorkplace.active_operator);
                        MakeFrame(false, newWorkplace.active_operator, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name);
                        // If worker is not logged in anywhere else, set login to FALSE
                        if (!CheckForWorkerLoggedIn(connection, newWorkplace.active_operator))
                        {
                            change_login_status(connection, newWorkplace.active_operator, false);
                        }
                    }
                    if(newWorkplace.workplace_unique && CheckNull(connection, newWorkplace.workplace_id))
                    {
                        change_workplace_worker(connection, newWorkplace.workplace_id, newWorkplace.sibling_workplace, swipeWorker.worker_id, 0);
                    }
                    
                    change_login_status(connection, swipeWorker.worker_id, true);
                    MakeFrame(true, swipeWorker.worker_id, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name);
                    change_worker_workplace(connection, swipeWorker.worker_id, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name, newWorkplace.workplace_id, newWorkplace.sibling_workplace);
                }           

            }
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **FRAME**
        // These functions generate and/or send the Sisteplant Captor Frames
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public void MakeFrame(bool status, int worker_id, string workplace_name, string second_workplace_name)
        {
            string Frame = "";
            // send in frame (single/double)
            if (status)
            {
                Frame = "INPW," + worker_id + "," + workplace_name.Trim();
                if (second_workplace_name != string.Empty && second_workplace_name != "")
                {
                    Frame += "," + second_workplace_name;
                }
            }
            // send out frame (single/double)
            if (!status)
            {
                Frame = "OUTW," + worker_id + "," + workplace_name.Trim();
                if (second_workplace_name != string.Empty && second_workplace_name != "")
                {
                    Frame += "," + second_workplace_name;
                }
            }
            //send_frame(Frame); // Disabled for testing
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
                StreamWriter sw = new StreamWriter("Swipelog.txt", true);
                sw.WriteLine("("+DateTime.Now.ToString("s")+"):\t" + frame);
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
        public void getEntry(QC.SqlConnection connection, SwipeEntry swipe)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT sent_workplace, sent_id, timestamp
                FROM SwipeData
                WHERE entry_number = " + GlobalVar.StartSwipe + ";";

                QC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    swipe.sent_workplace = reader.SafeGetInt(0);
                    swipe.sent_id = reader.SafeGetInt(1);
                    swipe.timestamp = reader.GetDateTime(2);
                }
                reader.Close();
        
            }
        }

        public void change_login_status(QC.SqlConnection connection, int worker_id, bool status)
        {
            // CHANGE IN DATABASE 
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                UPDATE Workers 
                SET login_status = '" + status + "'" +
                "WHERE worker_id = " + worker_id + ";";
                command.ExecuteNonQuery();
                //Console.WriteLine("Change Log Status - Executed");
            }
        }

        public bool CheckExist(QC.SqlConnection connection, Workers worker)
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

        public bool CheckNull(QC.SqlConnection connection, int id)
        {
            bool null_check = false;
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT active_operator
                FROM Workplaces
                WHERE workplace_id = " + id + ";";
                QC.SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    null_check = reader.IsDBNull(0);
                }
                reader.Close();
                return null_check;
            }
        }
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **WORKER FUNCTIONS**
        // These functions deal with the worker table in the database
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        public void change_worker_workplace(QC.SqlConnection connection, int worker_id, string workplace_name, string second_workplace_name, int workplace_id, int secondary_workplace_id)
        {
            using (var command = new QC.SqlCommand())
            {
                if (second_workplace_name == string.Empty)
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    UPDATE Workers 
                    SET workplace_name = '" + workplace_name +
                        "', secondary_workplace_name = NULL"  +  
                        ", workplace_id = " + workplace_id +
                        ", secondary_workplace_id = NULL"  + 
                        " " +
                        "WHERE worker_id = " + worker_id + ";";
                    command.ExecuteNonQuery();
                    //Console.WriteLine("Change Worker - Executed");
                }
                else
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    UPDATE Workers 
                    SET workplace_name = '" + workplace_name +
                        "', secondary_workplace_name = '" + second_workplace_name +
                        "', workplace_id = " + workplace_id +
                        ", secondary_workplace_id = " + secondary_workplace_id +
                        "WHERE worker_id = " + worker_id + ";";
                    command.ExecuteNonQuery();
                    //Console.WriteLine("Change Worker - Executed");
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
                            worker.worker_name = reader.SafeGetString(1);
                            worker.worker_clearance = reader.SafeGetInt(2);
                            worker.workplace_id = reader.SafeGetInt(3);
                            worker.workplace_name = reader.SafeGetString(4);
                            worker.secondary_workplace_id = reader.SafeGetInt(5);
                            worker.secondary_workplace_name = reader.SafeGetString(6);
                            worker.login_status = reader.GetBoolean(7);
                }
                reader.Close();
            }
        }
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // **WORKPLACE FUNCTIONS**
        // These functions deal with the workplace table in the database
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        public void change_workplace_worker(QC.SqlConnection connection, int workplace_id, int sibling_id, int worker_id, int old_worker_id)
        {
            if (worker_id == 0)
            {
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                        UPDATE Workplaces
                        SET active_operator = NULL " +
                        "WHERE active_operator = " + old_worker_id + 
                        "AND (workplace_id = " + workplace_id + " OR workplace_id = " + sibling_id + ");";
                    command.ExecuteNonQuery();
                    //Console.WriteLine("Change Workplace - Executed");
                }
            }
            else if(old_worker_id == 0)
            {
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                UPDATE Workplaces
                SET active_operator = " + worker_id +
                "WHERE workplace_id = " + workplace_id + 
                " OR workplace_id = " + sibling_id + ";";
                command.ExecuteNonQuery();
                //Console.WriteLine("Change Workplace - Executed");
                }
            }
            else
            {
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                        UPDATE Workplaces
                        SET active_operator = " + worker_id + 
                        "WHERE active_operator = " + old_worker_id +
                        "AND (workplace_id = " + workplace_id + " OR workplace_id = " + sibling_id + ");";
                            command.ExecuteNonQuery();
                    //Console.WriteLine("Change Workplace - Executed");
                }
            }
        }

        public bool CheckForWorkerLoggedIn(QC.SqlConnection connection, int id)
        {
            using (var command = new QC.SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = DT.CommandType.Text;
                command.CommandText = @"
                SELECT * FROM Workplaces WHERE active_operator = " + id + ";";
                QC.SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Close();
                    return true;
                }
                else
                {
                    reader.Close();
                    return false;
                }
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
