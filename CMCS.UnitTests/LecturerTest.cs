using CMCS;
using CMCS.Models;
using System.Data.SqlClient;

namespace CMCS.UnitTests
{
    [TestClass]
    public class LecturerTest
    {
        // Connection String for CMCS database
        private const string CONNECTION_STRING = "Server=tcp:cmcs-sql-server-st10069070.database.windows.net,1433;Initial Catalog=cmcs-db;Persist Security Info=False;User ID=cmcs-admin;Password=server333#1;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        [TestMethod]
        public async Task Test_RegisterUser()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Variable Declarations
            string firstName = "John";
            string lastName = "Lucas";
            string emailAddress = "john@example.com";
            string identityNumber = "11102";
            string password = "abcd12!@";

            // Obtain the lecturer id.
            int lecturerId = await CMCSDB.FindLecturer(identityNumber);

            if(lecturerId == 0)
            {
                string sql = $"INSERT INTO Lecturer(FirstName, LastName, EmailAddress, IdentityNumber, Password) VALUES ('{firstName}', '{lastName}', '{emailAddress}', '{identityNumber}', '{password}')";
                await CMCSDB.RunSQLNoResult(sql);

                // The test succeeded.
                Assert.IsTrue(true);
            }
            else
            {
                // The test failed.
                Assert.Fail("The user account already exists.");
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_UserLogin()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Variable Declarations
            string userId = "11102";
            string password = "abcd12!@";

            // Obtain the lecturer id.
            int lecturerId = await CMCSDB.FindLecturer(userId);

            // Check if the lecturer id is not zero.
            if (lecturerId != 0)
            {
                string sql = $"SELECT Password FROM Lecturer WHERE LecturerID = {lecturerId}";
                SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                string? userPassword = string.Empty;

                if (reader != null && reader.Read())
                {
                    userPassword = reader["Password"].ToString();
                }

                // Close the SqlDataReader object.
                await CMCSDB.CloseReader();
                // The test succeeded.
                Assert.IsTrue(password == userPassword);
            }
            else
            {
                // The test failed.
                Assert.Fail("The user account does not exist.");
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_SubmitClaim()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Variable Declarations
            string requestFor = "Bonus";
            string hoursWorked = "24";
            string hourlyRate = "12";
            string description = "Bonus1234";
            string dateSubmitted = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            int lecturerId = await CMCSDB.FindLecturer("11102");

            string sql = $"INSERT INTO Request(RequestFor, HoursWorked, HourlyRate, Description, DateSubmitted, RequestStatus, LecturerID) VALUES ('{requestFor}', '{hoursWorked}', '{hourlyRate}', '{description}', '{dateSubmitted}', 'Pending', {lecturerId})";
            // Run the SQL query.
            await CMCSDB.RunSQLNoResult(sql);

            // The test succeeded.
            Assert.IsTrue(true);

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_EditClaim()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection
            await CMCSDB.OpenConnection();

            // Obtain the lecturer id.
            int lecturerId = await CMCSDB.FindLecturer("11102");

            string sql = $"UPDATE Request SET RequestFor = 'Bonus1234' WHERE LecturerID = {lecturerId}";
            // Run the SQL query.
            await CMCSDB.RunSQLNoResult(sql);

            // The test succeeded.
            Assert.IsTrue(true);

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_CancelClaim()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Provide a valid request id here.
            int requestId = 58;

            string sql = $"DELETE FROM Request WHERE RequestID = {requestId}";
            // Run the SQL query.
            await CMCSDB.RunSQLNoResult(sql);

            // The test succeeded.
            Assert.IsTrue(true);

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_TrackClaim_IsPending()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Provide a valid request id here.
            int requestId = 0;

            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            // Declare and instantiate a SqlDataReader object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            if(reader != null && reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();

                // Test
                Assert.AreEqual(status, "Pending");
            }
            else
            {
                // Fail
                Assert.Fail();
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_TrackClaim_IsApproved()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Provide a valid request id here.
            int requestId = 0;

            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            // Declare and instantiate a SqlDataReader object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            if (reader != null && reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();

                // Test
                Assert.AreEqual(status, "Approved");
            }
            else
            {
                // Fail
                Assert.Fail();
            }

            // Close the database connection
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_TrackClaim_IsRejected()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Provide a valid request id here.
            int requestId = 0;

            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            // Declare and instantiate a SqlDataReader object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            if (reader != null && reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();

                // Test
                Assert.AreEqual(status, "Rejected");
            }
            else
            {
                // Fail
                Assert.Fail();
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }
    }
}