using CMCS.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Data.SqlClient;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace CMCS.Controllers
{
    public class UserAccountController : Controller
    {
        [HttpGet]
        [HttpPost]
        public IActionResult UserLogin()
        {
            if(this.Request.Method == "POST")
            {
                CMCSDB.OpenConnection();

                var sUserCredentials = new StreamReader(this.Request.Body).ReadToEndAsync().Result;
                dynamic? userCredentials = JsonConvert.DeserializeObject(sUserCredentials);

                string? userId = Convert.ToString(userCredentials?.UserId);
                string? userPassword = Convert.ToString(userCredentials?.Password);

                if (CMCSMain.UserExists(userId))
                {
                    string? password = string.Empty;

                    var sqlResult = CMCSDB.RunSQLResult($"SELECT * FROM Lecturer WHERE IdentityNumber = '{userId}'");

                    if (!sqlResult.HasRows)
                    {
                        sqlResult.Close();
                        sqlResult = CMCSDB.RunSQLResult($"SELECT * FROM Manager WHERE IdentityNumber = '{userId}'");
                        sqlResult.Read();

                        if(sqlResult.HasRows)
                        {
                            password = Convert.ToString(sqlResult["Password"]);
                            CMCSMain.User.IsManager = true;
                        }
                    }
                    else
                    {
                        sqlResult.Read();
                        password = Convert.ToString(sqlResult["Password"]);
                    }

                    if(userPassword == password)
                    {
                        CMCSMain.User.FirstName = Convert.ToString(sqlResult["FirstName"]);
                        CMCSMain.User.LastName = Convert.ToString(sqlResult["LastName"]);
                        CMCSMain.User.IdentityNumber = Convert.ToString(sqlResult["IdentityNumber"]);
                        CMCSMain.User.EmailAddress = Convert.ToString(sqlResult["EmailAddress"]);
                        CMCSMain.User.LoggedOn = true;

                        if (!CMCSMain.User.IsManager)
                        {
                            // The request succeeded.
                            this.Response.StatusCode = 1;
                        }
                        else
                        {
                            // The request succeeded.
                            this.Response.StatusCode = 3;
                        }
                    }
                    else
                    {
                        // The request failed.
                        this.Response.StatusCode = 2;
                    }

                    sqlResult.Close();
                }
                else
                {
                    // The request failed.
                    this.Response.StatusCode = 2;
                }
                CMCSDB.CloseConnection();
            }

            return View();
        }

        [HttpGet]
        [HttpPost]
        public IActionResult UserRegistration()
        {
            if(this.Request.Method == "POST")
            {
                // Obtain the user data as a JSON string.
                var sUserData = new StreamReader(this.Request.Body).ReadToEndAsync().Result;
                // Convert the JSON string to an anonymous object.
                dynamic? userData = JsonConvert.DeserializeObject(sUserData);

                // Get the user details.
                string? userAccountType = Convert.ToString(userData?.UserAccountType);
                string? userFirstName = Convert.ToString(userData?.FirstName);
                string? userLastName = Convert.ToString(userData?.LastName);
                string? userIdentityNumber = Convert.ToString(userData?.IdentityNumber);
                string? userEmailAddress = Convert.ToString(userData?.EmailAddress);
                string? userPassword = Convert.ToString(userData?.Password);

                dynamic? qualificationDocuments = userData?.QualificationDocuments;
                dynamic? identityDocuments = userData?.IdentityDocuments;

                // Declare and instantiate generic collections.
                List<CMCSDocument> QualificationDocuments = new List<CMCSDocument>();
                List<CMCSDocument> IdentityDocuments = new List<CMCSDocument>();

                // Iterate through the 'qualificationDocuments' array.
                for(int i = 0; i < qualificationDocuments?.Count; i++)
                {
                    dynamic? document = qualificationDocuments[i];

                    string documentName = Convert.ToString(document.Name);
                    int documentSize = Convert.ToInt32(document.Size);
                    string documentContent = Convert.ToString(document.Content);

                    QualificationDocuments.Add(new CMCSDocument
                    {
                        Name = documentName,
                        Size = documentSize,
                        Type = CMCSMain.GetDocumentType(documentName),
                        Content = documentContent

                    });
                }

                // Iterate through the 'identityDocuments' array.
                for(int i = 0; i < identityDocuments?.Count; i++)
                {
                    dynamic? document = identityDocuments[i];

                    string documentName = Convert.ToString(document.Name);
                    int documentSize = Convert.ToInt32(document.Size);
                    string documentContent = Convert.ToString(document.Content);

                    IdentityDocuments.Add(new CMCSDocument
                    {
                        Name = documentName,
                        Size = documentSize,
                        Type = CMCSMain.GetDocumentType(documentName),
                        Content = documentContent
                    });
                }

                CMCSDB.OpenConnection();

                // Check if the user does not exist.
                if (!CMCSMain.UserExists(userIdentityNumber))
                {
                    RegisterUser(userAccountType == "STANDARD", userFirstName, userLastName, userIdentityNumber, userEmailAddress, userPassword, QualificationDocuments.ToArray(), IdentityDocuments.ToArray());

                    // The request succeeded.
                    this.Response.StatusCode = 1;
                }
                else
                {
                    // The request failed.
                    this.Response.StatusCode = 2;
                }

                CMCSDB.CloseConnection();
            }

            return View();
        }

        private void RegisterUser(bool bLecturer, string firstName, string lastName, string identityNumber, string emailAddress, string password, CMCSDocument[] qualificationDocuments, CMCSDocument[] identityDocuments)
        {
            if (bLecturer)
            {
                CMCSDB.RunSQLNoResult($"INSERT INTO Lecturer(FirstName, LastName, IdentityNumber, EmailAddress, Password) VALUES ('{firstName}', '{lastName}', '{identityNumber}', '{emailAddress}', '{password}')");

                for(int i = 0; i < qualificationDocuments.Length; i++)
                {
                    var document = qualificationDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'QUALIFICATION', '{identityNumber}', '{document.Content}')");
                }

                for (int i = 0; i < identityDocuments.Length; i++)
                {
                    var document = identityDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'IDENTITY', '{identityNumber}', '{document.Content}')");
                }
            }
            else
            {
                CMCSDB.RunSQLNoResult($"INSERT INTO Manager(FirstName, LastName, IdentityNumber, EmailAddress, Password) VALUES ('{firstName}', '{lastName}', '{identityNumber}', '{emailAddress}', '{password}')");

                for (int i = 0; i < qualificationDocuments.Length; i++)
                {
                    var document = identityDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'QUALIFICATION', '{identityNumber}', '{document.Content}')");
                }

                for (int i = 0; i < identityDocuments.Length; i++)
                {
                    var document = identityDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'IDENTITY', '{identityNumber}', '{document.Content}')");
                }
            }
        }

        [HttpGet]
        public IActionResult UserLogout()
        {
            CMCSMain.User.FirstName = string.Empty;
            CMCSMain.User.LastName = string.Empty;
            CMCSMain.User.IdentityNumber = string.Empty;
            CMCSMain.User.EmailAddress = string.Empty;
            CMCSMain.User.IsManager = false;
            CMCSMain.User.LoggedOn = false;

            CMCSMain.SelectedRequestIndex = -1;
            CMCSMain.SelectedRequestID = 0;

            return RedirectToAction("UserLogin", "UserAccount");
        }


        private void UserAccount_SetRequestIndex()
        {
            CMCSMain.SelectedRequestIndex = Convert.ToInt32(this.Request.Headers["RowSelected"]);

            int lecturerId = CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
            var reader = CMCSDB.RunSQLResult($"SELECT * FROM Request WHERE LecturerID = {lecturerId}");

            for (int i = 0; i < CMCSMain.SelectedRequestIndex; i++)
                reader.Read();

            reader.Read();
            CMCSMain.SelectedRequestID = Convert.ToInt32(reader["RequestID"].ToString());

            reader.Close();

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        private void UserAccount_GetRequestIndex()
        {
            dynamic? requestData = new { RequestIndex = CMCSMain.SelectedRequestIndex, RequestID = CMCSMain.SelectedRequestID};

            this.Response.WriteAsync(JsonConvert.SerializeObject((object)requestData));
            this.Response.CompleteAsync();
        }

        private void UserAccount_GetSupportingDocuments()
        {
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"]);
            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            int row_count = CMCSDB.CountRows(sql);
            SqlDataReader reader = CMCSDB.RunSQLResult(sql);

            List<CMCSDocument> documentList = new List<CMCSDocument>();

            for (int i = 0; i < row_count; i++)
            {
                reader.Read();

                var document = new CMCSDocument();
                document.DocumentID = Convert.ToInt32(reader["DocumentID"]);
                document.Name = reader["Name"].ToString();
                document.Size = Convert.ToInt32(reader["Size"]);
                document.Type = reader["Type"].ToString();
                document.Section = reader["Section"].ToString();
                document.Content = reader["Content"].ToString();

                documentList.Add(document);
            }

            reader.Close();

            this.Response.WriteAsync(JsonConvert.SerializeObject(documentList.ToArray()));
            this.Response.CompleteAsync();
            this.Response.StatusCode = 1;
        }

        private void UserAccount_GetDocumentContent()
        {
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"]);
            int fileIndex = Convert.ToInt32(this.Request.Headers["FileIndex"]);

            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            SqlDataReader reader = CMCSDB.RunSQLResult(sql);

            for (int i = 0; i < fileIndex; i++)
                reader.Read();

            reader.Read();
            string filename = reader["Name"].ToString();
            string documentContent = reader["Content"].ToString();
            reader.Close();

            this.Response.WriteAsync($"[{filename}]{documentContent}");
            this.Response.CompleteAsync();
        }

        [HttpGet]
        [HttpPost]
        public IActionResult UserAccount()
        {
            CMCSDB.OpenConnection();

            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "SetRequestIndex")
                    UserAccount_SetRequestIndex();
            }

            if (this.Request.Method == "GET")
            {
                if (this.Request.Headers["ActionName"] == "GetSupportingDocuments")
                    UserAccount_GetSupportingDocuments();

                if (this.Request.Headers["ActionName"] == "GetDocumentContent")
                    UserAccount_GetDocumentContent();

                if (this.Request.Headers["ActionName"] == "GetRequestIndex")
                    UserAccount_GetRequestIndex();

                // The request succeeded.
                this.Response.StatusCode = 1;
            }

            int lecturerID = CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
            string sql = $"SELECT * FROM Request WHERE LecturerID = {lecturerID}";
            int rowCount = CMCSDB.CountRows(sql);
            SqlDataReader reader = CMCSDB.RunSQLResult(sql);
            List<CMCSRequest> requestList = new List<CMCSRequest>();

            if (reader.HasRows)
            {
                for(int i = 0; i < rowCount; i++)
                {
                    reader.Read();

                    CMCSRequest request = new CMCSRequest();
                    request.RequestID = Convert.ToInt32(reader["RequestID"]);
                    request.LecturerID = Convert.ToInt32(reader["LecturerID"]);
                    request.RequestFor = Convert.ToString(reader["RequestFor"]);
                    request.HoursWorked = Convert.ToInt32(reader["HoursWorked"]);
                    request.HourlyRate = Convert.ToInt32(reader["HourlyRate"]);
                    request.Description = Convert.ToString(reader["Description"]);
                    request.RequestStatus = Convert.ToString(reader["RequestStatus"]);
                    request.DateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString());

                    requestList.Add(request);
                }
            }

            reader.Close();

            for(int i = 0; i < requestList.Count; i++)
            {
                string sql2 = $"SELECT * FROM RequestProcess WHERE RequestID = {requestList[i].RequestID}";
                SqlDataReader reader2 = CMCSDB.RunSQLResult(sql2);

                if (reader2.HasRows)
                {
                    reader2.Read();
                    requestList[i].DateApproved = DateTime.Parse(reader2["Date"].ToString());
                }
                else
                {
                    requestList[i].DateApproved = null;
                }

                reader2.Close();
            }

            CMCSDB.CloseConnection();

            return View(requestList);
        }

        [HttpGet]
        [HttpPost]
        public IActionResult ViewDocument()
        {
            CMCSDB.OpenConnection();

            int requestID = Convert.ToInt32(this.Request.Query["RequestID"]);
            int documentIndex = Convert.ToInt32(this.Request.Query["DocumentIndex"]);

            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            SqlDataReader reader = CMCSDB.RunSQLResult(sql);

            for (int i = 0; i < documentIndex; i++)
                reader.Read();

            reader.Read();

            CMCSDocument document = new CMCSDocument();
            document.DocumentID = Convert.ToInt32(reader["DocumentID"].ToString());
            document.Name = reader["Name"].ToString();
            document.Size = Convert.ToInt32(reader["Size"].ToString());
            document.Type = reader["Type"].ToString();
            document.Section = reader["Section"].ToString();
            document.Content = reader["Content"].ToString();

            reader.Close();
            CMCSDB.CloseConnection();

            return View(document);
        }

        public IActionResult UpdateUserProfile()
        {
            return View();
        }

        public IActionResult UserForgotPassword()
        {
            return View();
        }
    }
}
