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
    SwipeData currentSwipe = new SwipeData();
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
            while(GlobalVar.SwipeNum>0)
            {
                getEntry(connectionB, currentSwipe);

                //Console.WriteLine("Writing Frame for Last Swipe");
                //Console.WriteLine("ID - " + currentSwipe.sent_id);
                //Console.WriteLine("Workplace - " + currentSwipe.sent_workplace);
                //Console.WriteLine("Start Swipe - " + GlobalVar.StartValue);
                //Console.WriteLine("SwipeNum - " + GlobalVar.SwipeNum);

                SwipeProcess(connectionA, currentSwipe);
                worker.ReportProgress(0);
                Thread.Sleep(100);
            }
        }                                    
        connectionA.Close();
        connectionB.Close();
    }
}

        public void SwipeProcess(QC.SqlConnection connection, SwipeData swipe)
        {
            bool processComplete = false; 

            // Set up "shells" for database data
            Workers swipeWorker = new Workers();
            swipeWorker.worker_id = swipe.sent_id;

            Workplaces newWorkplace = new Workplaces();
            newWorkplace.workplace_id = swipe.sent_workplace;

            Workplaces oldWorkplace = new Workplaces();

            // Fill worker "shell"
            FillWorker(connection, swipeWorker);
            FillWorkplace(connection, newWorkplace);
            oldWorkplace.workplace_id = swipeWorker.workplace_id;
            FillWorkplace(connection, oldWorkplace);

            // Check if ID exists
            // If not, add to the workers database
            if (!CheckExist(connection, swipeWorker.worker_id))
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
            // Check if Workplace exists
            // If not, bad scan, dump it.

            // What are we doing? Logging in or logging out?

            if(swipeWorker.worker_id!=0)
            {
                // Logout of old workplace if necessary
                if (swipeWorker.login_status)
                {
                    if(oldWorkplace.workplace_id == newWorkplace.workplace_id||oldWorkplace.workplace_id == newWorkplace.sibling_workplace)
                    {
                        change_login_status(connection, swipeWorker.worker_id, false);
                        MakeFrame(false, swipeWorker.worker_id, oldWorkplace.workplace_name, oldWorkplace.sibling_workplace_name);
                        processComplete = true;
                    }
                    if(!processComplete &&(newWorkplace.workplace_exclusive || oldWorkplace.workplace_exclusive))
                    {
                        change_login_status(connection, swipeWorker.worker_id, false);
                        MakeFrame(false, swipeWorker.worker_id, oldWorkplace.workplace_name, oldWorkplace.sibling_workplace_name);
                    }
                }
                // If new workplace is unique
                if (!processComplete && newWorkplace.workplace_unique && newWorkplace.active_operator != 0)
                {
                    // log out current worker
                    change_login_status(connection, newWorkplace.active_operator, false);
                    MakeFrame(false, newWorkplace.active_operator, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name); 
                }
                // Log into new workplace
                if(!processComplete)
                {
                    change_login_status(connection, swipeWorker.worker_id, true);
                    MakeFrame(true, swipeWorker.worker_id, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name);
                    change_worker_workplace(connection, swipeWorker.worker_id, newWorkplace.workplace_name, newWorkplace.sibling_workplace_name, newWorkplace.workplace_id, newWorkplace.sibling_workplace);
                    if (newWorkplace.workplace_unique)
                    {
                        change_workplace_worker(connection, swipeWorker.worker_id, newWorkplace.active_operator);
                    }
                    processComplete = true;
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
    //Console.WriteLine(Frame); 
    send_frame(Frame); // Disabled for testing
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
        StreamWriter sw = new StreamWriter("log.txt", true);
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
public void getEntry(QC.SqlConnection connection, SwipeData swipe)
{
    using (var command = new QC.SqlCommand())
    {
        command.Connection = connection;
        command.CommandType = DT.CommandType.Text;
        command.CommandText = @"
        SELECT sent_workplace, sent_id
        FROM SwipeData
        WHERE entry_number = " + GlobalVar.StartValue + ";";

        QC.SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            swipe.sent_workplace = reader.SafeGetInt(0);
            swipe.sent_id = reader.SafeGetInt(1);
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
public bool CheckExist(QC.SqlConnection connection, int id)
{
    int exist_id = 0;
    using (var command = new QC.SqlCommand())
    {
        command.Connection = connection;
        command.CommandType = DT.CommandType.Text;
        command.CommandText = @"
        SELECT * 
        FROM Workers
        WHERE worker_id = " + id + ";";
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
            worker.worker_clearance = reader.SafeGetInt(1);
            worker.workplace_id = reader.SafeGetInt(2);
            worker.workplace_name = reader.SafeGetString(3);
            worker.secondary_workplace_id = reader.SafeGetInt(4);
            worker.secondary_workplace_name = reader.SafeGetString(5);
            worker.login_status = reader.GetBoolean(6);
        }
        reader.Close();
    }
}
// -----------------------------------------------------------------------------------------------------------------------------------------------------
// **WORKPLACE FUNCTIONS**
// These functions deal with the workplace table in the database
// -----------------------------------------------------------------------------------------------------------------------------------------------------
public void change_workplace_worker(QC.SqlConnection connection, int worker_id, int old_worker_id)
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
                "" +
                "WHERE active_operator = " + old_worker_id + ";";
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
                SET active_operator = '" + worker_id +
                        "'WHERE active_operator = '" + old_worker_id + "';";
            command.ExecuteNonQuery();
            //Console.WriteLine("Change Workplace - Executed");
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
            workplace.active_maintenance = reader.SafeGetInt(3);
            workplace.active_engineer = reader.SafeGetInt(4);
            workplace.active_reference = reader.SafeGetString(5);
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
