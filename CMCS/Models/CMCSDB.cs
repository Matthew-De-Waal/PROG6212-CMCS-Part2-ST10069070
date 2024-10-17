using System.Data.SqlClient;
using System.Data;
using System.Transactions;

namespace CMCS.Models
{
    public static class CMCSDB
    {
        // Data fields
        private static SqlConnection? sqlConnection;
        private static bool readerOpened = false;
        private static SqlDataReader? sqlDataReader = null;

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
        /// Runs a SQL query that does not return a SqlDataReader? object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        public static void RunSQLNoResult(string sql)
        {
            if (!readerOpened)
            {
                SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
                sqlCmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Runs a SQL query and returns a SqlDataReader? object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns></returns>
        public static SqlDataReader? RunSQLResult(string sql)
        {
            if (!readerOpened)
            {
                readerOpened = true;
                SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);

                try
                {
                    sqlDataReader = sqlCmd.ExecuteReader();
                }
                catch
                {
                    Thread.Sleep(500);
                    return null;
                }

                return sqlDataReader;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Runs a SQL query and does not return a SqlDataReader? object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns>The output from the SQL query.</returns>
        public static object? RunSQLResultScalar(string sql)
        {
            if (!readerOpened)
            {
                SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
                return sqlCmd.ExecuteScalar();
            }
            else
            {
                return null;
            }
        }

        public static void CloseReader()
        {
            if(sqlDataReader != null && readerOpened)
            {
                readerOpened = false;
                sqlDataReader?.Close();
                sqlDataReader = null;
            }
        }

        /// <summary>
        /// This method counts the number of rows from a SQL SELECT statement.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns>The number of rows counted.</returns>
        public static int CountRows(string sql)
        {
            SqlDataReader? reader = RunSQLResult(sql);
            int rowCount = 0;

            while(reader != null && reader.Read())
            {
                rowCount++;
            }

            CMCSDB.CloseReader();

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
            SqlDataReader? reader = RunSQLResult(sql);
            int lecturerId = 0;

            if (reader != null && reader.Read())
            {
                lecturerId = Convert.ToInt32(reader["LecturerID"]);
            }

            CMCSDB.CloseReader();

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
            SqlDataReader? reader = RunSQLResult(sql);
            int managerId = 0;

            if (reader != null && reader.Read())
            {
                managerId = Convert.ToInt32(reader["ManagerID"]);
            }
            
            CMCSDB.CloseReader();

            return managerId;
        }
    }
}
