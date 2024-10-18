using System.Data.SqlClient;
using System.Data;
using System.Transactions;

namespace CMCS.Models
{
    public static class CMCSDB
    {
        // Data fields
        public static SqlConnection? sqlConnection;
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
        public static async Task OpenConnection()
        {
            if(sqlConnection != null && sqlConnection.State == ConnectionState.Closed)
            {
                 await sqlConnection.OpenAsync();
            }
        }

        /// <summary>
        /// Closes the CMCS database connection.
        /// </summary>
        public static async Task CloseConnection()
        {
            if(sqlConnection != null && sqlConnection.State == ConnectionState.Open)
            {
                await sqlConnection.CloseAsync();
            }
        }

        /// <summary>
        /// Runs a SQL query that does not return a SqlDataReader? object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        public static async Task RunSQLNoResult(string sql)
        {
            if (!readerOpened)
            {
                SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
                await sqlCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Runs a SQL query and returns a SqlDataReader? object.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns></returns>
        public static async Task<SqlDataReader?> RunSQLResult(string sql)
        {
            if (!readerOpened)
            {
                readerOpened = true;
                SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
                sqlDataReader = await sqlCmd.ExecuteReaderAsync();

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
        public static async Task<object?> RunSQLResultScalar(string sql)
        {
            if (!readerOpened)
            {
                SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
                object? result = await sqlCmd.ExecuteScalarAsync();

                return result;
            }
            else
            {
                return null;
            }
        }

        public static async Task CloseReader()
        {
            if(sqlDataReader != null && readerOpened)
            {
                await sqlDataReader.CloseAsync();
                sqlDataReader = null;
            }

            readerOpened = false;
        }

        /// <summary>
        /// This method counts the number of rows from a SQL SELECT statement.
        /// </summary>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <returns>The number of rows counted.</returns>
        public static async Task<int> CountRows(string sql)
        {
            SqlDataReader? reader = await RunSQLResult(sql);
            int rowCount = 0;

            while(reader != null && reader.Read())
            {
                rowCount++;
            }

            await CloseReader();

            return rowCount;
        }

        /// <summary>
        /// This method gets the LecturerID value from a provided identity number.
        /// </summary>
        /// <param name="identityNumber"></param>
        /// <returns></returns>
        public static async Task<int> FindLecturer(string? identityNumber)
        {
            string sql = $"SELECT LecturerID FROM Lecturer WHERE IdentityNumber = '{identityNumber}'";
            SqlDataReader? reader = await RunSQLResult(sql);
            int lecturerId = 0;

            if (reader != null && reader.Read())
            {
                lecturerId = Convert.ToInt32(reader["LecturerID"]);
            }

            await CloseReader();

            return lecturerId;
        }

        /// <summary>
        /// This method gets the ManagerID value from a provided identity number.
        /// </summary>
        /// <param name="identityNumber"></param>
        /// <returns></returns>
        public static async Task<int> FindManager(string? identityNumber)
        {
            string sql = $"SELECT ManagerID FROM Manager WHERE IdentityNumber = '{identityNumber}'";
            SqlDataReader? reader = await RunSQLResult(sql);
            int managerId = 0;

            if (reader != null && reader.Read())
            {
                managerId = Convert.ToInt32(reader["ManagerID"]);
            }
            
            await CloseReader();

            return managerId;
        }
    }
}
