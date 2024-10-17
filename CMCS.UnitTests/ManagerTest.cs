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

            int managerId = CMCSDB.FindManager(identityNumber);

            if (managerId == 0)
            {
                string sql = $"INSERT INTO Manager(FirstName, LastName, EmailAddress, IdentityNumber, Password) VALUES ('{firstName}', '{lastName}', '{emailAddress}', '{identityNumber}', '{password}')";
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

            int managerId = CMCSDB.FindManager(userId);

            if (managerId != 0)
            {
                string sql = $"SELECT Password FROM Manager WHERE ManagerID = {managerId}";
                SqlDataReader? reader = CMCSDB.RunSQLResult(sql);

                string? userPassword = string.Empty;

                if (reader != null && reader.Read())
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
        public void Test_ApproveClaim()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            int requestId = 0;
            string sql = $"UPDATE Request SET RequestStatus = 'Approved' WHERE RequestID = {requestId}";
            CMCSDB.RunSQLNoResult(sql);

            CMCSDB.CloseConnection();
        }

        [TestMethod]
        public void Test_RejectClaim()
        {
            CMCSDB.Initialize(CONNECTION_STRING);
            CMCSDB.OpenConnection();

            int requestId = 0;
            string sql = $"UPDATE Request SET RequestStatus = 'Rejected' WHERE RequestID = {requestId}";
            CMCSDB.RunSQLNoResult(sql);

            CMCSDB.CloseConnection();
        }
    }
}
