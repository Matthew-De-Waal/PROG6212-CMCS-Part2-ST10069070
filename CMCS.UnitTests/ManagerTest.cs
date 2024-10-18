using CMCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace CMCS.UnitTests
{
    [TestClass]
    public class ManagerTest
    {
        // Connection String for CMCS database.
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

            // Obtain the manager id.
            int managerId = await CMCSDB.FindManager(identityNumber);

            // Check if the manager id is zero (The manager account does not exist.)
            if (managerId == 0)
            {
                string sql = $"INSERT INTO Manager(FirstName, LastName, EmailAddress, IdentityNumber, Password) VALUES ('{firstName}', '{lastName}', '{emailAddress}', '{identityNumber}', '{password}')";
                // Run the SQL query.
                await CMCSDB.RunSQLNoResult(sql);

                // Test
                Assert.IsTrue(true);
            }
            else
            {
                // Fail
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
            string? password = "abcd12!@";

            // Obtain the manager id.
            int managerId = await CMCSDB.FindManager(userId);

            // Check if the manager id is not zero.
            if (managerId != 0)
            {
                string sql = $"SELECT Password FROM Manager WHERE ManagerID = {managerId}";
                // Declare and instantiate a SqlDataReader object.
                SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                string? userPassword = string.Empty;

                if (reader != null && reader.Read())
                {
                    userPassword = reader["Password"].ToString();
                }

                // Close the SqlDataReader object.
                await CMCSDB.CloseReader();

                // Test
                Assert.IsTrue(password == userPassword);
            }
            else
            {
                // Fail
                Assert.Fail("The user account does not exist.");
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_ApproveClaim()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Provide a valid request id here.
            int requestId = 0;

            string sql = $"UPDATE Request SET RequestStatus = 'Approved' WHERE RequestID = {requestId}";
            // Run the SQL query.
            await CMCSDB.RunSQLNoResult(sql);

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        [TestMethod]
        public async Task Test_RejectClaim()
        {
            // Database initialization
            CMCSDB.Initialize(CONNECTION_STRING);
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Provide a valid request id jere.
            int requestId = 0;

            string sql = $"UPDATE Request SET RequestStatus = 'Rejected' WHERE RequestID = {requestId}";
            // Run the SQL query.
            await CMCSDB.RunSQLNoResult(sql);

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }
    }
}
