using System.Data.SqlClient;
using System.Data;

namespace CMCS.Models
{
    public static class CMCSDB
    {
        // Data fields
        private static SqlConnection? sqlConnection;

        /// <summary>
        /// Initializes the CMCS database connection.
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string? connectionString)
        {
            sqlConnection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Opens the CMCS database connection.
        /// </summary>
        public static void OpenConnection()
        {
            if(sqlConnection != null && sqlConnection.State == ConnectionState.Closed)
            {
                sqlConnection.Open();
            }
        }

        /// <summary>
        /// Closes the CMCS database connection.
        /// </summary>
        public static void CloseConnection()
        {
            if(sqlConnection != null && sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Runs a SQL query that does not return a SqlDataReader object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        public static void RunSQLNoResult(string sql)
        {
            SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
            sqlCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Runs a SQL query and returns a SqlDataReader object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns></returns>
        public static SqlDataReader RunSQLResult(string sql)
        {
            SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
            return sqlCmd.ExecuteReader();
        }

        /// <summary>
        /// Runs a SQL query and does not return a SqlDataReader object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns>The output from the SQL query.</returns>
        public static object RunSQLResultScalar(string sql)
        {
            SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
            return sqlCmd.ExecuteScalar();
        }

        /// <summary>
        /// This method counts the number of rows from a SQL SELECT statement.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns>The number of rows counted.</returns>
        public static int CountRows(string sql)
        {
            SqlDataReader reader = RunSQLResult(sql);
            int rowCount = 0;

            while(reader.Read())
            {
                rowCount++;
            }

            reader.Close();

            return rowCount;
        }

        /// <summary>
        /// This method gets the LecturerID value from a provided identity number.
        /// </summary>
        /// <param name="identityNumber"></param>
        /// <returns></returns>
        public static int FindLecturer(string? identityNumber)
        {
            string sql = $"SELECT LecturerID FROM Lecturer WHERE IdentityNumber = '{identityNumber}'";
            SqlDataReader reader = RunSQLResult(sql);
            int lecturerId = 0;

            if (reader.Read())
            {
                lecturerId = Convert.ToInt32(reader["LecturerID"]);
            }

            reader.Close();

            return lecturerId;
        }

        /// <summary>
        /// This method gets the ManagerID value from a provided identity number.
        /// </summary>
        /// <param name="identityNumber"></param>
        /// <returns></returns>
        public static int FindManager(string identityNumber)
        {
            string sql = $"SELECT ManagerID FROM Manager WHERE IdentityNumber = '{identityNumber}'";
            SqlDataReader reader = RunSQLResult(sql);
            int managerId = 0;

            if (reader.Read())
            {
                managerId = Convert.ToInt32(reader["ManagerID"]);
            }
            
            reader.Close();

            return managerId;
        }
    }
}
