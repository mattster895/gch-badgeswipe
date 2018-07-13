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
using SQLReaderExtensions;
using System.IO;

namespace BadgeSwipeApp
{
    class RefreshFromFile
    {
        // Refresh Workplace References - COMPLETE IN LASERS
        // Refresh Workplace Workers - TO BE IMPLEMENTED
        // Refresh Reference Details - TO BE IMPLEMENTED

        public void RefreshLaserWorkplace()
        {
            // -------------------------------------------------------------------------------------------------------------------------------------
            // Read in Sisteplant's query export
            // -------------------------------------------------------------------------------------------------------------------------------------

            string filepath = "workplaces.xlsx";
            DataSet queryExport = new DataSet();
            List<int> removeList = new List<int>();

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
                                rowReader.Read();
                                rowReader.Read();
                                rowReader.Read();
                                rowReader.Read();
                            }
                        }
                    });

                    queryExport = result;

                    // The result of each spreadsheet is in result.Tables
                }

                // ------------------------------------------------------------------------------------------------------------------------------
                // Separate Lasers by headstocks
                // ------------------------------------------------------------------------------------------------------------------------------

                // Check headstock column
                for (int i = 0; i < queryExport.Tables.Count; i++)
                {
                    DataTable workingTable = queryExport.Tables[i];

                    for(int j = 0; j<workingTable.Rows.Count; j++)
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
                            workingTable.Rows.InsertAt(duplicateRow, j+1);
                        }
                    }
                }

                // ------------------------------------------------------------------------------------------------------------------------------
                // Do some basic formatting (remove date column, trim order version off of reference)
                // ------------------------------------------------------------------------------------------------------------------------------

                char[] orderVersionTrim = { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

                foreach(DataTable table in queryExport.Tables)
                {
                    table.Columns.Remove("LOG IN DATE");
                    table.Columns.Add("REFERENCE NUMBER");
                    foreach (DataRow row in table.Rows)
                    {
                        if(row["HEADSTOCK"].ToString().Contains("SIDE A"))
                        {
                            row["Workplace"] += " A";
                        }
                        else
                        {
                            row["Workplace"] += " B";
                        }

                        if(row["MANUFACTURING ORDER"].ToString().Equals("WR"))
                        {
                            row["REFERENCE NUMBER"] = 0;
                        }
                        else
                        {
                            row["MANUFACTURING ORDER"] = row["MANUFACTURING ORDER"].ToString().TrimEnd(orderVersionTrim);
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
                        foreach(DataTable table in queryExport.Tables)
                        {

                            foreach(DataRow row in table.Rows)
                            {
                                if(!row["MANUFACTURING ORDER"].ToString().Equals("WR"))
                                {
                                    command.Connection = connectionMainDB;
                                    command.CommandType = DT.CommandType.Text;
                                    command.CommandText = @"
                                    SELECT reference_number
                                    FROM Refs
                                    WHERE manufacturing_reference LIKE '%" + row["MANUFACTURING ORDER"] + "%';";

                                    QC.SqlDataReader reader = command.ExecuteReader();

                                    while (reader.Read())
                                    {
                                        if (reader.SafeGetInt(0) != 0)
                                        {
                                            row["REFERENCE NUMBER"] = reader.SafeGetInt(0);
                                        }
                                    }
                                    reader.Close();
                                }
                            }

                            table.Columns.Remove("MANUFACTURING ORDER");

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

                            foreach(int entry in removeList.Reverse<int>())
                            {
                                table.Rows.RemoveAt(entry);
                            }
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
                    foreach(DataTable table in queryExport.Tables)
                    {
                        foreach(DataRow row in table.Rows)
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
