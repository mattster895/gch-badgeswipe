using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DT = System.Data;
using QC = System.Data.SqlClient;


namespace SQLReaderExtensions
{
    // Extension methods must be deined in a static class
    public static class MyExtensions
    {
        public static string SafeGetString(this SqlDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
                return reader.GetString(colIndex);
            return string.Empty;
        }



        public static int SafeGetInt(this SqlDataReader reader, int colIndex)
        {
            if (!reader.isDBNull(colIndex))
                return reader.GetInt32(colIndex);
            return 0;
        }
    }
}
