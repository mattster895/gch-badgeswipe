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
                                        row["REFERENCE NUMBER"] = reader.SafeGetInt(0);
                                        reader.Close();
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
                                            row["REFERENCE NUMBER"] = reader.SafeGetInt(0);
                                            reader.Close();
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

                            else if (Convert.ToInt32(table.Rows[i]["REFERENCE NUMBER"]) > 28)
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
            Console.WriteLine("To Be Implemented");
        }

        public void RefreshReferenceDetails()
        {
            Console.WriteLine("To Be Implemented");
        }

    }
}
