using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using QC = System.Data.SqlClient;
using DT = System.Data;
using MyExtensions;
using System.IO;

namespace BadgeSwipeApp
{
    class RefreshFromFile
    {
        // Refresh Workplace References - COMPLETE IN LASERS
        // Refresh Workplace Workers - TO BE IMPLEMENTED
        // Refresh Reference Details - TO BE IMPLEMENTED

        public DataSet QueryToDataSet(string filepath, int headerRows)
        {
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Use the AsDataSet extension method
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = true,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            EmptyColumnNamePrefix = "Column",
                            UseHeaderRow = true,
                            ReadHeaderRow = (rowReader) =>
                            {
                                for (int i = 0; i < headerRows; i++)
                                {
                                    rowReader.Read();
                                }
                            }
                        }
                    });

                    return result;
                }
            }
        }

        public void removeRows(DataTable table, List<int> removeList)
        {
            foreach (int entry in removeList.Reverse<int>())
            {
                table.Rows.RemoveAt(entry);
            }
        }

        public void addRefs(DataTable table, List<int> addList)
        {
            using (var connectionMainDBAdd = new QC.SqlConnection(
            "Server = 192.168.176.133; " +
            "Database=Badge_Swipe_MainDB;" +
            "Trusted_Connection=yes;"))
            {
                connectionMainDBAdd.Open();
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connectionMainDBAdd;
                    command.CommandType = DT.CommandType.Text;
                    foreach(int entry in addList)
                    {
                        command.CommandText = @"
                        INSERT INTO Refs(manufacturing_reference, order_version) VALUES('" + 
                        table.Rows[entry]["MANUFACTURING ORDER"].ToString() + "', '" + 
                        table.Rows[entry]["ORDER VERSION"].ToString().Truncate(4) + "');";
                        command.ExecuteNonQuery();
                    }
                }
                connectionMainDBAdd.Close();
            }
        }

        public bool altRefCheck(string alt_order)
        {
            using (var connectionMainDBCheck = new QC.SqlConnection(
                "Server = 192.168.176.133; " +
                "Database=Badge_Swipe_MainDB; " +
                "Trusted_Connection=yes;"))
            {
                connectionMainDBCheck.Open();
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connectionMainDBCheck;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    SELECT * FROM AltRefs WHERE alt_order LIKE '" + alt_order + "';";
                    QC.SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
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
        }

        public void addAltRefs(int standard_ref, int alt_version, bool alt_ref, bool ghost_ref, string alt_order)
        {
            using (var connectionMainDBAdd = new QC.SqlConnection(
                "Server = 192.168.176.133; " +
                "Database=Badge_Swipe_MainDB; " +
                "Trusted_Connection=yes;"))
            {
                connectionMainDBAdd.Open();
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connectionMainDBAdd;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = @"
                    INSERT INTO AltRefs(standard_ref, alt_version, alt_ref, ghost_ref, alt_order) VALUES('" +
                    standard_ref + "', '" +
                    alt_version + "', '" +
                    alt_ref + "', '" +
                    ghost_ref + "', '" +
                    alt_order + "');";
                    command.ExecuteNonQuery();
                }
                connectionMainDBAdd.Close();
            }
        }

        public void RefreshLaserWorkplace()
        {
            // Declare variables
            string workplaceExportPath = "workplaces.xlsx";
            int workplaceHeaderRows = 4;
            DataSet queryExport = new DataSet();
            List<int> removeList = new List<int>();
            List<int> addList = new List<int>();

            // Read in Sisteplant's query export
            queryExport = QueryToDataSet(workplaceExportPath, workplaceHeaderRows);

            // Separate Lasers by headstocks
            // Check headstock column
            for (int i = 0; i < queryExport.Tables.Count; i++)
            {
                DataTable workingTable = queryExport.Tables[i];

                for (int j = 0; j < workingTable.Rows.Count; j++)
                {
                    DataRow workingRow = workingTable.Rows[j];

                    // If this is a row that has both headstocks logged
                    if (workingRow["HEADSTOCK"].ToString().Equals("|SIDE A|SIDE B|"))
                    {
                        // Change headstock to SIDE A
                        workingRow["HEADSTOCK"] = "|SIDE A|";
                        
                        // Create a duplicate row
                        DataRow duplicateRow = workingTable.NewRow();
                        duplicateRow.ItemArray = workingRow.ItemArray.Clone() as object[];
                        
                        // Change duplicate headstock to SIDE B
                        duplicateRow["HEADSTOCK"] = "|SIDE B|";
                        workingTable.Rows.InsertAt(duplicateRow, j + 1);
                    }
                }
            }

            // Do some basic formatting 
            foreach (DataTable table in queryExport.Tables)
            {
                // Remove date column, it's useless here
                table.Columns.Remove("LOG IN DATE");

                // Add two new columns for reference number and order version (split from manufacturing order)
                table.Columns.Add("ORDER VERSION");
                table.Columns.Add("REFERENCE NUMBER");
                table.Columns.Add("REFERENCE VERSION");

                foreach (DataRow row in table.Rows)
                {
                    // Append headstock ID to workplace
                    if (row["HEADSTOCK"].ToString().Contains("SIDE A"))
                    {
                        row["WORKPLACE"] += " A";
                    }
                    else
                    {
                        row["WORKPLACE"] += " B";
                    }

                    // If manufacturing order is WR, set reference number to 0 (empty)
                    if (row["MANUFACTURING ORDER"].ToString().Equals("WR"))
                    {
                        row["REFERENCE NUMBER"] = 0;
                        row["REFERENCE VERSION"] = 0;
                    }

                    else
                    {
                        int truncAt = 0;
                        foreach (char c in row["MANUFACTURING ORDER"].ToString())
                        {
                            if (c == '-')
                                break;
                            truncAt++;
                        }
                        int cutAt = truncAt + 1;
                        row["ORDER VERSION"] = row["MANUFACTURING ORDER"].ToString().Cut(cutAt);
                        row["MANUFACTURING ORDER"] = row["MANUFACTURING ORDER"].ToString().Truncate(truncAt);
                    }
                }
                table.Columns.Remove("HEADSTOCK");
            }


            // ------------------------------------------------------------------------------------------------------------------------------
            // Match references to ref IDs
            // ------------------------------------------------------------------------------------------------------------------------------

            // Connect to Badge_Swipe_MainDB.Refs
            // Use data reader to send queries

            // Search sql like 
            // If no matching reference, delete row

            using (var connectionMainDB = new QC.SqlConnection(
           "Server = 192.168.176.133; " +
           "Database=Badge_Swipe_MainDB;" +
           "Trusted_Connection=yes;"))
            {
                connectionMainDB.Open();
                using (var command = new QC.SqlCommand())
                {
                    foreach (DataTable table in queryExport.Tables)
                    {

                        foreach (DataRow row in table.Rows)
                        {
                            if (!row["MANUFACTURING ORDER"].ToString().Equals("WR"))
                            {
                                string mOrder = row["MANUFACTURING ORDER"].ToString();
                                string match5 = mOrder.Truncate(5);
                                string match4 = mOrder.Truncate(4);

                                // Match Criteria
                                // ---------------------------------------------------------------------------------------
                                // First, look for exact match
                                // Next, look at the first 5 digits (ghost) - 'Lxxxx' - Ex. L3259A066F matches L32591066F
                                // Then, look at the first 4 digits (alt) - 'Lxxx' - Ex. L621TX066F matches L6214D066F
                                // Finally, just make a new reference and email someone

                                command.Connection = connectionMainDB;
                                command.CommandType = DT.CommandType.Text;
                                command.CommandText = @"
                                    SELECT reference_number
                                    FROM Refs
                                    WHERE manufacturing_reference LIKE '" + mOrder + "';"; // Exact match

                                QC.SqlDataReader reader = command.ExecuteReader();

                                if (reader.Read())
                                {
                                    if (reader.SafeGetInt(0) != 0)
                                    {
                                        row["REFERENCE NUMBER"] = reader.SafeGetInt(0);
                                        row["REFERENCE VERSION"] = 0;
                                    }
                                    reader.Close();
                                }
                                else
                                {
                                    reader.Close();
                                    command.CommandText = @"
                                        SELECT reference_number
                                        FROM Refs
                                        WHERE manufacturing_reference LIKE '" + match5 + "%';"; // Ghost Match
                                    reader = command.ExecuteReader();

                                    if (reader.Read())
                                    {
                                        Console.WriteLine("Ghost Match - " + mOrder);
                                        Console.WriteLine("Logging in Reference # " + reader.SafeGetInt(0));
                                        row["REFERENCE NUMBER"] = reader.SafeGetInt(0);
                                        int tempRef = reader.SafeGetInt(0);
                                        reader.Close();

                                        // check for entry in the AltRefs table

                                        if (!altRefCheck(mOrder))
                                        {
                                            // add new entry into AltRefs table
                                            // add standard ref, add the manufacturing order, mark as a ghost

                                            command.CommandText = @"
                                        SELECT alt_version
                                        FROM AltRefs
                                        WHERE standard_ref = " + tempRef +
                                            " ORDER BY alt_version DESC;";
                                            reader = command.ExecuteReader();

                                            if (reader.Read())
                                            {
                                                addAltRefs(tempRef, reader.SafeGetInt(0)+1, false, true, mOrder);
                                                row["REFERENCE VERSION"] = reader.SafeGetInt(0) + 1;
                                                reader.Close();
                                            }
                                            else
                                            {
                                                addAltRefs(tempRef, 1, false, true, mOrder);
                                                reader.Close();
                                                row["REFERENCE VERSION"] = 1;
                                            }
                                        }
                                        else
                                        {
                                            command.CommandText = @"
                                                SELECT alt_version
                                                FROM AltRefs
                                                WHERE alt_order LIKE '" + mOrder + "';";
                                            reader = command.ExecuteReader();
                                            while (reader.Read())
                                            {
                                                row["REFERENCE VERSION"] = reader.SafeGetInt(0);
                                            }
                                            reader.Close();
                                        }
                                    }
                                    else
                                    {
                                        reader.Close();
                                        command.CommandText = @"
                                            SELECT reference_number
                                            FROM Refs
                                            WHERE manufacturing_reference LIKE '" + match4 + "%';"; // Alt Match
                                        reader = command.ExecuteReader();

                                        if (reader.Read())
                                        {
                                            Console.WriteLine("Alt Match - " + mOrder);
                                            Console.WriteLine("Logging in Reference # " + reader.SafeGetInt(0));
                                            row["REFERENCE NUMBER"] = reader.SafeGetInt(0);
                                            int tempRef = reader.SafeGetInt(0);
                                            reader.Close();

                                            if (!altRefCheck(mOrder))
                                            {
                                                // add new entry into AltRefs table
                                                // add standard ref, add the manufacturing order, mark as a ghost

                                                command.CommandText = @"
                                                SELECT alt_version
                                                FROM AltRefs
                                                WHERE standard_ref = " + tempRef +
                                                " ORDER BY alt_version DESC;";
                                                reader = command.ExecuteReader();

                                                if (reader.Read())
                                                {
                                                    addAltRefs(tempRef, reader.SafeGetInt(0) + 1, true, false, mOrder);
                                                    row["REFERENCE VERSION"] = reader.SafeGetInt(0) + 1;
                                                    reader.Close();
                                                }
                                                else
                                                {
                                                    addAltRefs(tempRef, 1, true, false, mOrder);
                                                    reader.Close();
                                                    row["REFERENCE VERSION"] = 1;
                                                }
                                            }
                                            else
                                            {

                                                command.CommandText = @"
                                                SELECT alt_version
                                                FROM AltRefs
                                                WHERE alt_order LIKE '" + mOrder + "';";
                                                reader = command.ExecuteReader();
                                                while (reader.Read())
                                                {
                                                    row["REFERENCE VERSION"] = reader.SafeGetInt(0);
                                                }
                                                reader.Close();
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("No Match - " + mOrder);
                                            Console.WriteLine("Adding new entry");
                                            // Add new entry to table
                                            addList.Add(table.Rows.IndexOf(row));
                                            reader.Close();
                                        }
                                    }
                                }
                            }
                        }

                        addRefs(table, addList);

                        //table.Columns.Remove("MANUFACTURING ORDER");
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            if (table.Rows[i]["REFERENCE NUMBER"].GetType() == typeof(DBNull))
                            {

                                removeList.Add(i);
                            }

                            else if (Convert.ToInt32(table.Rows[i]["REFERENCE NUMBER"]) > 29)
                            {

                                removeList.Add(i);
                            }

                        }
                        removeRows(table, removeList);
                    }
                }
                connectionMainDB.Close();
            }

            // ------------------------------------------------------------------------------------------------------------------------------
            // Update Badge_Swipe_MainDB.Workplaces 
            // ------------------------------------------------------------------------------------------------------------------------------

            // copy working DataSet to Badge_Swipe_MainDB.Workplaces Laser DataSet

            using (var connectionMainDB = new QC.SqlConnection(
            "Server = 192.168.176.133; " +
            "Database=Badge_Swipe_MainDB;" +
            "Trusted_Connection=yes;"))
            {
                connectionMainDB.Open();
                foreach (DataTable table in queryExport.Tables)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        using (var command = new QC.SqlCommand())
                        {
                            command.Connection = connectionMainDB;
                            command.CommandType = DT.CommandType.Text;
                            command.CommandText = @"
                                UPDATE Workplaces
                                SET active_reference = " + row["REFERENCE NUMBER"] +
                                ", active_reference_version = " + row["REFERENCE VERSION"] +
                                " WHERE workplace_name = '" + row["WORKPLACE"] + "';";

                            command.ExecuteNonQuery();
                        }
                    }
                }
                connectionMainDB.Close();
            }

            foreach (DataTable table in queryExport.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        Console.Write(row[column] + "  ");
                    }
                    Console.WriteLine();
                }
            }
        }

        public void RefreshWorkplaceWorkers()
        {
            // Declare variables
            string workerExportPath = "workers.xlsx";
            int workerHeaderRows = 4;
            string workplace_name;
            int worker_id;
            int workplace_id = 0;
            DataSet queryExport = new DataSet();

            // Read in Sisteplant's query export
            queryExport = QueryToDataSet(workerExportPath, workerHeaderRows);

            

            using (var connectionMainDBAdd = new QC.SqlConnection(
            "Server = 192.168.176.133; " +
            "Database=Badge_Swipe_MainDB;" +
            "Trusted_Connection=yes;"))
            {
                connectionMainDBAdd.Open();
                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connectionMainDBAdd;
                    command.CommandType = DT.CommandType.Text;
                    
                    // Clear workplaces table
                    // ------------------------
                    command.CommandText = @"
                    UPDATE Workplaces
                    SET active_operator = 0;";
                    command.ExecuteNonQuery();

                    // Clear workers table
                    // ------------------------
                    command.CommandText = @"
                    UPDATE Workers
                    SET workplace_id = 0,
                    workplace_name = NULL,
                    login_status = 0;";
                    command.ExecuteNonQuery();

                }
                connectionMainDBAdd.Close();
            }

            

            // 1. For each entry in the query export
            foreach(DataTable table in queryExport.Tables)
            {
                table.Columns.Remove("LOG IN DATE");
                foreach(DataRow row in table.Rows)
                {
                    // 2. Read in the workplace and operator
                    workplace_name = row["WORKPLACE"].ToString();
                    Int32.TryParse(row["WORKER"].ToString(),out worker_id);
                    

                    using (var connectionMainDBAdd = new QC.SqlConnection(
                    "Server = 192.168.176.133; " +
                    "Database=Badge_Swipe_MainDB;" +
                    "Trusted_Connection=yes;"))
                    {
                        connectionMainDBAdd.Open();
                        using (var command = new QC.SqlCommand())
                        {
                            command.Connection = connectionMainDBAdd;
                            command.CommandType = DT.CommandType.Text;

                            // 3. Set active_operator to listed where workplace is LIKE the one saved in Workplaces
                            command.CommandText = @"
                            UPDATE Workplaces
                            SET active_operator = " + worker_id + " WHERE workplace_name = '" + workplace_name + "';";
                            command.ExecuteNonQuery();

                            // 4. Save the corresponding workplace_id
                            QC.SqlDataReader reader;
                            command.CommandText = @"SELECT workplace_id FROM Workplaces WHERE workplace_name = '" + workplace_name + "';";
                            reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                workplace_id = reader.SafeGetInt(0);
                            }
                            
                            reader.Close();

                            // 5 Check Workers to see if worker exists.
                            command.CommandText = @"SELECT * FROM Workers WHERE worker_id = " + worker_id + ";";
                            reader = command.ExecuteReader();

                            // 6A If not, add them
                            if (!reader.HasRows)
                            {
                                reader.Close();
                                command.CommandText = @"INSERT INTO Workers(worker_id, worker_clearance, login_status) VALUES(" + worker_id + ",1,0);";
                                // SEND EMAIL TO HR
                                EmailAgent emailAgent = new EmailAgent();
                                emailAgent.MissingBadgeEmail(worker_id);
                                command.ExecuteNonQuery();
                            }

                            if (!reader.IsClosed) { reader.Close(); }
                                

                            // 6B Update Workers, for operator, set login to 1, set workplace_id to saved, set workplace_name to saved
                            command.CommandText = @"UPDATE Workers
                            SET workplace_id = " + workplace_id +
                            ", workplace_name = '" + workplace_name +
                            "', login_status = 1 WHERE worker_id = " + worker_id + ";";
                            command.ExecuteNonQuery();

                        }
                        connectionMainDBAdd.Close();
                    }
                }
            }
        }

        public void RefreshReferenceDetails()
        {
            Console.WriteLine("To Be Implemented");
        }

        public void DatasetPrint(DataSet printMe)
        {
            // Print
            foreach (DataTable table in printMe.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        Console.Write(row[column] + "  ");
                    }
                    Console.WriteLine();
                }
            }
        }

    }
}
