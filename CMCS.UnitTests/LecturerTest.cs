using CMCS;
using CMCS.Models;
using System.Data.SqlClient;

namespace CMCS.UnitTests
{
    [TestClass]
    public class LecturerTest
    {
        private const string CONNECTION_STRING = "Server=tcp:cmcs-sql-server-st10069070.database.windows.net,1433;Initial Catalog=cmcs-db;Persist Security Info=False;User ID=cmcs-admin;Password=server333#1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        [TestMethod]
        public void Test_RegisterUser()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            string firstName = "John";
            string lastName = "Lucas";
            string emailAddress = "john@example.com";
            string identityNumber = "11102";
            string password = "abcd12!@";

            int lecturerId = CMCSDB.FindLecturer(identityNumber);

            if(lecturerId == 0)
            {
                string sql = $"INSERT INTO Lecturer(FirstName, LastName, EmailAddress, IdentityNumber, Password) VALUES ('{firstName}', '{lastName}', '{emailAddress}', '{identityNumber}', '{password}')";
                CMCSDB.RunSQLNoResult(sql);

                Assert.IsTrue(true);
            }
            else
            {
                Assert.Fail("The user account already exists.");
            }

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_UserLogin()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            string userId = "11102";
            string password = "abcd12!@";

            int lecturerId = CMCSDB.FindLecturer(userId);

            if (lecturerId != 0)
            {
                string sql = $"SELECT Password FROM Lecturer WHERE LecturerID = {lecturerId}";
                SqlDataReader? reader = CMCSDB.RunSQLResult(sql);

                string userPassword = string.Empty;

                if (reader.Read())
                {
                    userPassword = reader["Password"].ToString();
                }

                CMCSDB.CloseReader();
                Assert.IsTrue(password == userPassword);
            }
            else
            {
                Assert.Fail("The user account does not exist.");
            }

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_SubmitClaim()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            string requestFor = "Bonus";
            string hoursWorked = "24";
            string hourlyRate = "12";
            string description = "Bonus1234";
            string dateSubmitted = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            int lecturerId = CMCSDB.FindLecturer("111");

            string sql = $"INSERT INTO Request(RequestFor, HoursWorked, HourlyRate, Description, DateSubmitted, RequestStatus, LecturerID) VALUES ('{requestFor}', '{hoursWorked}', '{hourlyRate}', '{description}', '{dateSubmitted}', 'Pending', {lecturerId})";
            CMCSDB.RunSQLNoResult(sql);

            Assert.IsTrue(true);

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_EditClaim()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            int lecturerId = CMCSDB.FindLecturer("11102");

            string sql = $"UPDATE Request SET RequestFor = 'Bonus1234' WHERE LecturerID = {lecturerId}";
            CMCSDB.RunSQLNoResult(sql);

            Assert.IsTrue(true);

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_CancelClaim()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            int requestId = 0;
            string sql = $"DELETE FROM Request WHERE RequestID = {requestId}";
            CMCSDB.RunSQLNoResult(sql);

            Assert.IsTrue(true);

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_TrackClaim_IsPending()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            // Provide a valid request id here.
            int requestId = 0;

            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            SqlDataReader? reader = CMCSDB.RunSQLResult(sql);

            if(reader != null && reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();
                Assert.AreEqual(status, "Pending");
            }
            else
            {
                Assert.Fail();
            }

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_TrackClaim_IsApproved()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            int requestId = 33;
            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            SqlDataReader? reader = CMCSDB.RunSQLResult(sql);

            if (reader != null && reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();
                Assert.AreEqual(status, "Approved");
            }
            else
            {
                Assert.Fail();
            }

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_TrackClaim_IsRejected()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            int requestId = 0;
            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            SqlDataReader? reader = CMCSDB.RunSQLResult(sql);

            if (reader != null && reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();
                Assert.AreEqual(status, "Rejected");
            }
            else
            {
                Assert.Fail();
            }

            CMCSDB.CloseConnection();
        }
    }
}