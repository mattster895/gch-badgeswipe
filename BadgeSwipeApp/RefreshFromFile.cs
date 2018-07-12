﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using System.IO;

namespace BadgeSwipeApp
{
    class RefreshFromFile
    {
        public void RefreshWorkplace()
        {
            // -------------------------------------------------------------------------------------------------------------------------------------
            // Read in Sisteplant's query export
            // -------------------------------------------------------------------------------------------------------------------------------------

            string filepath = "workplaces.xlsx";
            DataSet queryExport = new DataSet();

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
                    foreach(DataRow row in table.Rows)
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
                            row["MANUFACTURING ORDER"] = "NONE ATTACHED";
                        }
                        else
                        {
                            row["MANUFACTURING ORDER"] = row["MANUFACTURING ORDER"].ToString().TrimEnd(orderVersionTrim);
                        }
                    }
                    table.Columns.Remove("HEADSTOCK");
                }
                

                // ------------------------------------------------------------------------------------------------------------------------------
                // Match manufacturing orders to references
                // ------------------------------------------------------------------------------------------------------------------------------

                // Connect to Badge_Swipe_MainDB.Refs
                // Use data reader to send queries

                // Search sql like 
                // If no matching reference, delete row

                // ------------------------------------------------------------------------------------------------------------------------------
                // Collapse child references into parents
                // ------------------------------------------------------------------------------------------------------------------------------

                // For each workplace, 
                // If more than one reference,
                // for n references
                // child check:
                // if n has child,
                // delete child recursively

                // ------------------------------------------------------------------------------------------------------------------------------
                // Update Badge_Swipe_MainDB.Workplaces 
                // ------------------------------------------------------------------------------------------------------------------------------

                // confirm 24 entries (for laser)
                // copy working DataSet to Badge_Swipe_MainDB.Workplaces Laser DataSet

                // ------------------------------------------------------------------------------------------------------------------------------
                // Print for debugging
                // ------------------------------------------------------------------------------------------------------------------------------

                foreach (DataTable thisTable in queryExport.Tables)
                {
                    // For each row, print the values of each column.
                    foreach (DataRow row in thisTable.Rows)
                    {
                        foreach (DataColumn column in thisTable.Columns)
                        {
                            Console.Write(row[column] + " ");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
