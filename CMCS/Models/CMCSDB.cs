using System.Data.SqlClient;
using System.Data;

namespace CMCS.Models
{
    public static class CMCSDB
    {
        private static SqlConnection? sqlConnection;

        public static void Initialize(string? connectionString)
        {
            sqlConnection = new SqlConnection(connectionString);
        }

        public static void OpenConnection()
        {
            if(sqlConnection != null && sqlConnection.State == ConnectionState.Closed)
            {
                sqlConnection.Open();
            }
        }

        public static void CloseConnection()
        {
            if(sqlConnection != null && sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }
        }

        public static void RunSQLNoResult(string sql)
        {
            SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
            sqlCmd.ExecuteNonQuery();
        }

        public static SqlDataReader RunSQLResult(string sql)
        {
            SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
            return sqlCmd.ExecuteReader();
        }

        public static object RunSQLResultScalar(string sql)
        {
            SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);
            return sqlCmd.ExecuteScalar();
        }

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

        public static int FindLecturer(string? identityNumber)
        {
            string sql = $"SELECT LecturerID FROM Lecturer WHERE IdentityNumber = '{identityNumber}'";
            SqlDataReader reader = RunSQLResult(sql);
            reader.Read();

            int lecturerId = Convert.ToInt32(reader["LecturerID"]);
            reader.Close();

            return lecturerId;
        }

        public static int FindManager(string identityNumber)
        {
            string sql = $"SELECT ManagerID FROM Manager WHERE IdentityNumber = '{identityNumber}'";
            SqlDataReader reader = RunSQLResult(sql);
            reader.Read();

            int managerId = Convert.ToInt32(reader["ManagerID"]);
            reader.Close();

            return managerId;
        }
    }
}
