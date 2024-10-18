using CMCS.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using CMCS.DBContext;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    public class UserAccountController : Controller
    {
        // Data fields
        private readonly AppDbContext _context;

        /// <summary>
        /// Master constructor
        /// </summary>
        /// <param name="context"></param>
        public UserAccountController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// CMCS UserLogin page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> UserLogin()
        {
            // POST method.
            if (this.Request.Method == "POST")
            {
                // Open the database connection.
                await CMCSDB.OpenConnection();

                // Read from the request body, asynchronously.
                var sUserCredentials = await new StreamReader(this.Request.Body).ReadToEndAsync();
                dynamic? userCredentials = JsonConvert.DeserializeObject(sUserCredentials);

                // Get the fields from the dynamic object.
                string? userId = Convert.ToString(userCredentials?.UserId);
                string? userPassword = Convert.ToString(userCredentials?.Password);

                // Check if the user account exists.
                if (await CMCSMain.UserExists(userId))
                {
                    // Variable declaration.
                    string? password = string.Empty;

                    // Run the sql query and obtain the SqlDataReader? object.
                    var sqlResult = await CMCSDB.RunSQLResult($"SELECT * FROM Lecturer WHERE IdentityNumber = '{userId}'");
                    
                    if (sqlResult != null)
                    {
                        // Check if the SqlDataReader? object has no rows.
                        if (!sqlResult.HasRows)
                        {
                            // Close the SqlDataReader? object.
                            await CMCSDB.CloseReader();
                            sqlResult = await CMCSDB.RunSQLResult($"SELECT * FROM Manager WHERE IdentityNumber = '{userId}'");
                            // Read data from the SqlDataReader? object.
                            sqlResult?.Read();

                            // Check if the SqlDataReader? 'sqlResult' has rows.
                            if (sqlResult != null && sqlResult.HasRows)
                            {
                                // Obtain the password from the SqlDataReader? object.
                                password = Convert.ToString(sqlResult["Password"]);
                                CMCSMain.User.IsManager = true;
                            }
                        }
                        else
                        {
                            // Read data from the SqlDataReader? object.
                            sqlResult.Read();
                            password = Convert.ToString(sqlResult["Password"]);
                        }

                        // Check if the two passwords matches.
                        if (userPassword == password)
                        {
                            // Assign the user's information to the global properties.
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
                    }
                    else
                    {
                        this.Response.StatusCode = 2;
                    }

                    // Close the SqlDataReader? object.
                    await CMCSDB.CloseReader();
                }
                else
                {
                    // The request failed.
                    this.Response.StatusCode = 2;
                }

                // Close the database connection.
                await CMCSDB.CloseConnection();
            }

            return View();
        }

        /// <summary>
        /// CMCS UserRegistration page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> UserRegistration()
        {
            // POST method
            if (this.Request.Method == "POST")
            {
                // Obtain the user data as a JSON string.
                var sUserData = await new StreamReader(this.Request.Body).ReadToEndAsync();
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
                List<Document> QualificationDocuments = new List<Document>();
                List<Document> IdentityDocuments = new List<Document>();

                // Iterate through the 'qualificationDocuments' array.
                for (int i = 0; i < qualificationDocuments?.Count; i++)
                {
                    // Dynamic object: document
                    dynamic? document = qualificationDocuments[i];

                    // Obtain the fields from the dynamic object.
                    string documentName = Convert.ToString(document.Name);
                    int documentSize = Convert.ToInt32(document.Size);
                    string documentContent = Convert.ToString(document.Content);

                    // Add a new CMCSDocument to the collection.
                    QualificationDocuments.Add(new Document
                    {
                        Name = documentName,
                        Size = documentSize,
                        Type = CMCSMain.GetDocumentType(documentName),
                        Content = documentContent

                    });
                }

                // Iterate through the 'identityDocuments' array.
                for (int i = 0; i < identityDocuments?.Count; i++)
                {
                    // Dynamic object: document.
                    dynamic? document = identityDocuments[i];

                    // Obtain the fields from the dynamic object.
                    string documentName = Convert.ToString(document.Name);
                    int documentSize = Convert.ToInt32(document.Size);
                    string documentContent = Convert.ToString(document.Content);

                    // Add a new CMCDocument to the collection.
                    IdentityDocuments.Add(new Document
                    {
                        Name = documentName,
                        Size = documentSize,
                        Type = CMCSMain.GetDocumentType(documentName),
                        Content = documentContent
                    });
                }

                // Open the database connection.
                await CMCSDB.OpenConnection();

                // Check if the user does not exist.
                if (!(await CMCSMain.UserExists(userIdentityNumber)))
                {
                    await RegisterUser(userAccountType == "STANDARD", userFirstName, userLastName, userIdentityNumber, userEmailAddress, userPassword, QualificationDocuments.ToArray(), IdentityDocuments.ToArray());

                    // The request succeeded.
                    this.Response.StatusCode = 1;
                }
                else
                {
                    // The request failed.
                    this.Response.StatusCode = 2;
                }

                // Close the database connection.
                await CMCSDB.CloseConnection();
            }

            return View();
        }

        /// <summary>
        /// CMCS UserLogout page
        /// </summary>
        /// <returns></returns>
        public IActionResult UserLogout()
        {
            // Reset the global properties.
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

        /// <summary>
        /// CMCS UserAccount page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> UserAccount()
        {
            // Open the database connection.
            await CMCSDB.OpenConnection();
            List<Request> requestList = new List<Request>();

            if (!this.Request.Headers.ContainsKey("ActionName"))
            {
                int lecturerID = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
                string sql = $"SELECT * FROM Request WHERE LecturerID = {lecturerID}";
                int rowCount = await CMCSDB.CountRows(sql);
                SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                if (reader != null)
                {
                    // Check if the reader has rows.
                    if (reader.HasRows)
                    {
                        // Iterate through the collection.
                        for (int i = 0; i < rowCount; i++)
                        {
                            // Read data from the reader.
                            reader.Read();

                            // Create a CMCSRequest object.
                            Request request = new Request();
                            request.RequestID = Convert.ToInt32(reader["RequestID"]);
                            request.LecturerID = Convert.ToInt32(reader["LecturerID"]);
                            request.RequestFor = Convert.ToString(reader["RequestFor"]);
                            request.HoursWorked = Convert.ToInt32(reader["HoursWorked"]);
                            request.HourlyRate = Convert.ToInt32(reader["HourlyRate"]);
                            request.Description = Convert.ToString(reader["Description"]);
                            request.RequestStatus = Convert.ToString(reader["RequestStatus"]);
                            request.DateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString());

                            // Add the object to the requestList.
                            requestList.Add(request);
                        }
                    }
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();

                // Iterate through the 'requestList' collection.
                for (int i = 0; i < requestList.Count; i++)
                {
                    string sql2 = $"SELECT * FROM RequestProcess WHERE RequestID = {requestList[i].RequestID}";
                    SqlDataReader? reader2 = await CMCSDB.RunSQLResult(sql2);

                    // Check if the reader has rows.
                    if (reader2 != null && reader2.HasRows)
                    {
                        reader2.Read();
                        requestList[i].DateApproved = DateTime.Parse(reader2["Date"].ToString());
                    }
                    else
                    {
                        requestList[i].DateApproved = null;
                    }

                    // Close the SqlDataReader? object.
                    await CMCSDB.CloseReader();
                }
            }

            // POST method
            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "SetRequestIndex")
                    await UserAccount_SetRequestIndex();
            }

            // GET method
            if (this.Request.Method == "GET")
            {
                if (this.Request.Headers["ActionName"] == "GetUserAccountData")
                    await UserAccount_GetUserAccountData(false);

                if (this.Request.Headers["ActionName"] == "GetSupportingDocuments")
                    await UserAccount_GetSupportingDocuments();

                if (this.Request.Headers["ActionName"] == "GetDocumentContent")
                    await UserAccount_GetDocumentContent();

                if (this.Request.Headers["ActionName"] == "GetDocumentContent2")
                    UserAccount_GetDocumentContent2();

                if (this.Request.Headers["ActionName"] == "GetRequestIndex")
                    UserAccount_GetRequestIndex();

                if (this.Request.Headers["ActionName"] == "GetSecurityQuestion")
                    await UserAccount_GetSecurityQuestion();
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();

            return View(requestList);
        }

        /// <summary>
        /// CMCS ViewDocument page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ViewDocument()
        {
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Get the fields from the request headers.
            int requestID = Convert.ToInt32(this.Request.Query["RequestID"]);
            int documentIndex = Convert.ToInt32(this.Request.Query["DocumentIndex"]);

            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            // Declare and instantiate a SqlDataReader? object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);
            Document? document = null;

            if (reader != null)
            {
                // Skip through the collection.
                for (int i = 0; i < documentIndex; i++)
                    reader.Read();

                // Read data from the SqlDataReader? object.
                reader.Read();

                // Declare and instantiate a CMCSDocument object.
                document = new Document();
                document.DocumentID = Convert.ToInt32(reader["DocumentID"].ToString());
                document.Name = reader["Name"].ToString();
                document.Size = Convert.ToInt32(reader["Size"].ToString());
                document.Type = reader["Type"].ToString();
                document.Section = reader["Section"].ToString();
                document.Content = reader["Content"].ToString();
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
            // Close the database connection.
            await CMCSDB.CloseConnection();

            return View(document);
        }

        /// <summary>
        /// CMCS UpdateUserProfile page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> UpdateUserProfile()
        {
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // GET method
            if (this.Request.Method == "GET")
            {
                if (this.Request.Headers["ActionName"] == "GetUserAccountData")
                    await UserAccount_GetUserAccountData(true);

                if (this.Request.Headers["ActionName"] == "GetRecoveryFile")
                    UserAccount_GetRecoveryFile();
            }

            // POST method: UpdateAccount
            if (this.Request.Method == "POST" && this.Request.Headers["ActionName"] == "UpdateAccount")
            {
                // Read from the request body, asynchronously.
                string? requestBody = await new StreamReader(this.Request.Body).ReadToEndAsync();
                // Create an object from the request body, using JsonConvert.
                dynamic? userData = JsonConvert.DeserializeObject(requestBody);

                // Variable Declarations
                string? firstName = Convert.ToString(userData.FirstName);
                string? lastName = Convert.ToString(userData.LastName);
                string? identityNumber = Convert.ToString(userData.IdentityNumber);
                string? emailAddress = Convert.ToString(userData.EmailAddress);
                bool recoveryMethod1Enabled = Convert.ToBoolean(userData.RecoveryMethod1Enabled);
                bool recoveryMethod2Enabled = Convert.ToBoolean(userData.RecoveryMethod2Enabled);
                bool generateNewRecoveryFile = Convert.ToBoolean(userData.GenerateNewRecoveryFile);
                string? securityQuestion = Convert.ToString(userData.SecurityQuestion);
                string? securityAnswer = Convert.ToString(userData.SecurityAnswer);
                string? currentPassword = Convert.ToString(userData.CurrentPassword);
                string? newPassword = Convert.ToString(userData.NewPassword);
                string? newPasswordConfirmed = Convert.ToString(userData.NewPasswordConfirmed);

                // Dynamic objects.
                dynamic? qualificationDocuments = userData.QualificationDocuments;
                dynamic? identityDocuments = userData.IdentityDocuments;
                dynamic? deleted_qualification_documents = userData.DeletedQualificationDocuments;
                dynamic? deleted_identity_documents = userData.DeletedIdentityDocuments;

                // Check if the user is not a Manager.
                if (!CMCSMain.User.IsManager)
                {
                    int lecturerId = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
                    string sql = $"UPDATE Lecturer SET FirstName = '{firstName}', LastName = '{lastName}', IdentityNumber = '{identityNumber}', EmailAddress = '{emailAddress}' WHERE LecturerID = {lecturerId}";
                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult(sql);
                }
                else
                {
                    int managerId = await CMCSDB.FindManager(CMCSMain.User.IdentityNumber);
                    string sql = $"UPDATE Manager SET FirstName = '{firstName}', LastName = '{lastName}', IdentityNumber = '{identityNumber}', EmailAddress = '{emailAddress}' WHERE ManagerID = {managerId}";
                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult(sql);
                }

                // Check if 'Recovery Method 1' is enabled.
                if (recoveryMethod1Enabled)
                {
                    bool success = _context.AccountRecovery.Where(i => i.UserID == CMCSMain.User.IdentityNumber && i.Method == "FILE").ToList().Count > 0;

                    if (!success)
                    {
                        // Generate a random key of 30 characters.
                        string key = CMCSMain.GenerateKey(30);

                        AccountRecovery recovery = new AccountRecovery();
                        recovery.Method = "FILE";
                        recovery.Value = key;
                        recovery.UserID = CMCSMain.User.IdentityNumber;

                        _context.AccountRecovery.Add(recovery);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Check if a new recovery file must be generated.
                        if (generateNewRecoveryFile)
                        {
                            // Generate a random key of 30 characters.
                            string key = CMCSMain.GenerateKey(30);

                            AccountRecovery recovery = _context.AccountRecovery.Where(i => i.Method == "FILE" && i.UserID == CMCSMain.User.IdentityNumber).ToList()[0];
                            recovery.Value = key;
                            
                            _context.Update(recovery);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    var result = _context.AccountRecovery.Where(i => i.Method == "FILE" && i.UserID == CMCSMain.User.IdentityNumber).ToList();

                    if (result.Count > 0)
                    {
                        _context.Remove(result[0]);
                        await _context.SaveChangesAsync();
                    }
                }

                // Check if 'Recovery Method 2' is enabled.
                if (recoveryMethod2Enabled)
                {
                    bool success = (await _context.AccountRecovery.Where(i => i.UserID == CMCSMain.User.IdentityNumber && i.Method == "FILE").ToListAsync()).Count > 0;

                    // Check if the reader cannot read data.
                    if (!success)
                    {
                        AccountRecovery recovery = new AccountRecovery();
                        recovery.Method = "QUESTION";
                        recovery.Value = $"{securityQuestion};{securityAnswer}";
                        recovery.UserID = CMCSMain.User.IdentityNumber;

                        await _context.AddAsync(recovery);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        AccountRecovery recovery = _context.AccountRecovery.Where(i => i.Method == "QUESTION" && i.UserID == CMCSMain.User.IdentityNumber).ToList()[0];
                        recovery.Value = $"{securityQuestion};{securityAnswer}";

                        _context.Update(recovery);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var result = _context.AccountRecovery.Where(i => i.Method == "QUESTION" && i.UserID == CMCSMain.User.IdentityNumber).ToList();

                    if (result.Count > 0)
                    {
                        _context.Remove(result[0]);
                        await _context.SaveChangesAsync();
                    }
                }

                // Insert the uploaded qualification documents.
                for (int i = 0; i < qualificationDocuments.Count; i++)
                {
                    string documentName = Convert.ToString(qualificationDocuments[i].Name);
                    long documentSize = Convert.ToInt64(qualificationDocuments[i].Size);
                    string documentContent = Convert.ToString(qualificationDocuments[i].Content);

                    string sql = $"INSERT INTO Document(Name, Size, Type, Section, Content, UserID) VALUES ('{documentName}', {documentSize}, '{CMCSMain.GetDocumentType(documentName)}', 'QUALIFICATION', '{documentContent}', '{CMCSMain.User.IdentityNumber}')";
                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult(sql);
                }

                // Insert the uploaded identity documents.
                for (int i = 0; i < identityDocuments.Count; i++)
                {
                    string documentName = Convert.ToString(identityDocuments[i].Name);
                    long documentSize = Convert.ToInt64(identityDocuments[i].Size);
                    string documentContent = Convert.ToString(identityDocuments[i].Content);

                    string sql = $"INSERT INTO Document(Name, Size, Type, Section, Content, UserID) VALUES ('{documentName}', {documentSize}, '{CMCSMain.GetDocumentType(documentName)}', 'IDENTITY', '{documentContent}', '{CMCSMain.User.IdentityNumber}')";
                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult(sql);
                }

                if (Convert.ToString(deleted_qualification_documents) != "DELETE_ALL")
                {
                    // Delete the selected qualification documents.
                    for (int i = 0; i < deleted_qualification_documents.Count; i++)
                    {
                        int documentId = Convert.ToInt32(deleted_qualification_documents[i]);
                        string sql = $"DELETE FROM Document WHERE DocumentID = {documentId}";
                        // Run a SQL query.
                        await CMCSDB.RunSQLNoResult(sql);
                    }
                }
                else
                {
                    string sql = $"DELETE FROM Document WHERE UserID = '{CMCSMain.User.IdentityNumber}' AND Section = 'QUALIFICATION'";
                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult(sql);
                }

                if (Convert.ToString(deleted_identity_documents) != "DELETE_ALL")
                {
                    // Delete the selected identity documents.
                    for (int i = 0; i < deleted_qualification_documents.Count; i++)
                    {
                        int documentId = Convert.ToInt32(deleted_identity_documents[i]);
                        string sql = $"DELETE FROM Document WHERE DocumentID = {documentId}";
                        // Run a SQL query.
                        await CMCSDB.RunSQLNoResult(sql);
                    }
                }
                else
                {
                    string sql = $"DELETE FROM Document WHERE UserID = '{CMCSMain.User.IdentityNumber}' AND Section = 'IDENTITY'";
                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult(sql);
                }

                if(!string.IsNullOrEmpty(currentPassword) ||
                    !string.IsNullOrEmpty(newPassword) ||
                    !string.IsNullOrEmpty(newPasswordConfirmed))
                {
                    string sql = string.Empty;

                    if(!CMCSMain.User.IsManager)
                    {
                        int lecturerId = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
                        sql = $"SELECT * FROM Lecturer WHERE LecturerID = {lecturerId}";
                        SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                        bool success = false;

                        if(reader != null && reader.Read())
                        {
                            string? password = reader["Password"].ToString();
                            
                            if(currentPassword == password && newPassword == newPasswordConfirmed)
                            {
                                success = true;
                            }
                        }

                        await CMCSDB.CloseReader();

                        if (success)
                        {
                            string sql2 = $"UPDATE Lecturer SET Password = '{newPassword}' WHERE LecturerID = {lecturerId}";
                            await CMCSDB.RunSQLNoResult(sql2);
                        }
                    }
                    else
                    {
                        int managerId = await CMCSDB.FindManager(CMCSMain.User.IdentityNumber);
                        sql = $"SELECT * FROM Lecturer WHERE LecturerID = {managerId}";
                        SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                        bool success = false;

                        if (reader != null && reader.Read())
                        {
                            string? password = reader["Password"].ToString();

                            if (currentPassword == password && newPassword == newPasswordConfirmed)
                            {
                                success = true;
                            }
                        }

                        await CMCSDB.CloseReader();

                        if (success)
                        {
                            string sql2 = $"UPDATE Manager SET Password = '{newPassword}' WHERE ManagerID = {managerId}";
                            await CMCSDB.RunSQLNoResult(sql2);
                        }
                    }
                }

                // The request succeeded.
                this.Response.StatusCode = 1;
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();

            return View();
        }

        /// <summary>
        /// CMCS UserForgotPassword page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> UserForgotPassword()
        {
            // POST method: ResetPassword
            if (this.Request.Method == "POST" && this.Request.Headers["ActionName"] == "ResetPassword")
            {
                // Read data from the request body, asynchronously.
                string? requestBody = await new StreamReader(this.Request.Body).ReadToEndAsync();
                dynamic? userData = JsonConvert.DeserializeObject(requestBody);

                string? userId = Convert.ToString(userData.UserID);
                int recoveryMethod = Convert.ToInt32(userData.RecoveryMethod);
                string newPassword = Convert.ToString(userData.NewPassword);
                string newPasswordConfirmed = Convert.ToString(userData.NewPasswordConfirmed);
                bool success = false;

                // Open the database connection.
                await CMCSDB.OpenConnection();

                // Recovery Method 1: Recover from a recovery file.
                if (recoveryMethod == 1)
                {
                    // Variable Declarations
                    string recoveryFileContent = Convert.ToString(userData.RecoveryFileContent);

                    string sUserID = recoveryFileContent.Split(";")[0];
                    string sUserID2 = sUserID.Substring(sUserID.LastIndexOf("=") + 1);

                    string sRecoveryKey = recoveryFileContent.Split(";")[1];
                    string sRecoveryKey2 = sRecoveryKey.Substring(sRecoveryKey.LastIndexOf("=") + 1);

                    string sql = $"SELECT * FROM AccountRecovery WHERE Method = 'FILE' AND Value = '{sRecoveryKey2}' AND UserID = '{sUserID2}'";
                    // Declare and instantiate a SqlDataReader? object.
                    SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                    if (reader != null)
                        success = reader.Read() && userId == sUserID2;

                    // Close the reader.
                    await CMCSDB.CloseReader();
                }

                // Recovery Method 2: Recover with a Security Question.
                if (recoveryMethod == 2)
                {
                    string securityAnswer = Convert.ToString(userData.SecurityAnswer);

                    string sql = $"SELECT * FROM AccountRecovery WHERE Method = 'QUESTION' AND Value LIKE '%;{securityAnswer}' AND UserID = '{userId}'";
                    // Declare and instantiate a SqlDataReader? object.
                    SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                    if(reader != null)
                        success = reader.Read();

                    // Close the reader.
                    await CMCSDB.CloseReader();
                }

                if (success)
                {
                    // Obtain the lecturer id.
                    int userAccountId = await CMCSDB.FindLecturer(userId);

                    if (userAccountId != 0)
                    {
                        string sql = $"UPDATE Lecturer SET Password = '{newPassword}' WHERE LecturerID = {userAccountId}";
                        await CMCSDB.RunSQLNoResult(sql);

                        // The request succeeded.
                        this.Response.StatusCode = 1;
                    }
                    else
                    {
                        userAccountId = await CMCSDB.FindManager(userId);

                        if (userAccountId != 0)
                        {
                            string sql = $"UPDATE Manager SET Password = '{newPassword}' WHERE ManagerID = {userAccountId}";
                            await CMCSDB.RunSQLNoResult(sql);

                            // The request succeeded.
                            this.Response.StatusCode = 1;
                        }
                        else
                        {
                            // The request failed.
                            this.Response.StatusCode = 2;
                        }
                    }
                }
                else
                {
                    // The request failed.
                    this.Response.StatusCode = 2;
                }

                // Close the database connection.
                await CMCSDB.CloseConnection();
            }

            return View();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private async Task UserAccount_SetRequestIndex()
        {
            // Set the request index
            CMCSMain.SelectedRequestIndex = Convert.ToInt32(this.Request.Headers["RowSelected"]);

            // Get the lecturer id.
            int lecturerId = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
            var reader = await CMCSDB.RunSQLResult($"SELECT * FROM Request WHERE LecturerID = {lecturerId}");

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < CMCSMain.SelectedRequestIndex; i++)
                    reader.Read();

                // Read data from the SqlDataReader? object.
                reader.Read();
                CMCSMain.SelectedRequestID = Convert.ToInt32(reader["RequestID"].ToString());

                // The request succeeded.
                this.Response.StatusCode = 1;
            }
            else
            {
                this.Response.StatusCode = 2;
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private void UserAccount_GetRequestIndex()
        {
            dynamic? requestData = new { RequestIndex = CMCSMain.SelectedRequestIndex, RequestID = CMCSMain.SelectedRequestID };

            // The request succeeded.
            this.Response.StatusCode = 1;
            this.Response.WriteAsync(JsonConvert.SerializeObject((object)requestData));
            this.Response.CompleteAsync();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private async Task UserAccount_GetSupportingDocuments()
        {
            // Get the request id from the request header 'RequestID'.
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"]);
            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            int row_count = await CMCSDB.CountRows(sql);
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            // Create a generic collection instance.
            List<Document> documentList = new List<Document>();

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < row_count; i++)
                {
                    reader.Read();

                    // Create a CMCSDocument instance.
                    var document = new Models.Document();
                    document.DocumentID = Convert.ToInt32(reader["DocumentID"]);
                    document.Name = reader["Name"].ToString();
                    document.Size = Convert.ToInt32(reader["Size"]);
                    document.Type = reader["Type"].ToString();
                    document.Section = reader["Section"].ToString();
                    document.Content = reader["Content"].ToString();

                    // Add the object to the 'documentList' collection.
                    documentList.Add(document);
                }

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync(JsonConvert.SerializeObject(documentList.ToArray()));
                this.Response.CompleteAsync();
            }
            else
            {
                this.Response.StatusCode = 2;
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private async Task UserAccount_GetDocumentContent()
        {
            // Obtain the fields from the request headers 'RequestID' and 'FileIndex'.
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"]);
            int fileIndex = Convert.ToInt32(this.Request.Headers["FileIndex"]);

            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            // Declare and instantiate a SqlDataReader? object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < fileIndex; i++)
                    reader.Read();

                // Read data from the SqlDataReader? object.
                reader.Read();
                string filename = reader["Name"].ToString();
                string documentContent = reader["Content"].ToString();

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync($"[{filename}]{documentContent}");
                this.Response.CompleteAsync();
            }
            else
            {
                this.Response.StatusCode = 2;
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private void UserAccount_GetDocumentContent2()
        {
            int documentId = Convert.ToInt32(this.Request.Headers["DocumentID"]);
            Document document = _context.Document.Where(i => i.DocumentID == documentId).ToList()[0];

            this.Response.Headers.Add("FileName", document.Name);

            // The request succeeded.
            this.Response.StatusCode = 1;
            this.Response.WriteAsync(document.Content);
            this.Response.CompleteAsync();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private async Task UserAccount_GetUserAccountData(bool currentUser)
        {
            // Variable declarations
            string? firstName = string.Empty;
            string? lastName = string.Empty;
            string? identityNumber = string.Empty;
            string? emailAddress = string.Empty;

            SqlDataReader? reader = null;

            if (!CMCSMain.User.IsManager)
            {
                // Obtain the lecturer id
                int lecturerId = currentUser ? await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber) : Convert.ToInt32(this.Request.Headers["LecturerID"]);
                string sql = $"SELECT * FROM Lecturer WHERE LecturerID = {lecturerId}";
                reader = await CMCSDB.RunSQLResult(sql);

                // Check if the reader can read data.
                if (reader != null && reader.Read())
                {
                    firstName = reader["FirstName"].ToString();
                    lastName = reader["LastName"].ToString();
                    identityNumber = reader["IdentityNumber"].ToString();
                    emailAddress = reader["EmailAddress"].ToString();
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();
            }
            else
            {
                // Obtain the manager id
                int managerId = await CMCSDB.FindManager(CMCSMain.User.IdentityNumber);
                string sql = $"SELECT * FROM Manager WHERE ManagerID = {managerId}";
                reader = await CMCSDB.RunSQLResult(sql);

                // Check if the reader can read data.
                if (reader != null && reader.Read())
                {
                    firstName = reader["FirstName"].ToString();
                    lastName = reader["LastName"].ToString();
                    identityNumber = reader["IdentityNumber"].ToString();
                    emailAddress = reader["EmailAddress"].ToString();
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();
            }

            string? sql2 = string.Empty;

            if (currentUser)
            {
                sql2 = $"SELECT * FROM Document WHERE UserID = '{CMCSMain.User.IdentityNumber}'";
            }
            else
            {
                sql2 = $"SELECT * FROM Document d INNER JOIN Lecturer l ON l.IdentityNumber = d.UserID WHERE l.LecturerID = {this.Request.Headers["LecturerID"]}";
            }

            reader = await CMCSDB.RunSQLResult(sql2);

            // Generic collections.
            List<Document> qualificationDocuments = new();
            List<Document> identityDocuments = new();

            // Iterate through the collection.
            while (reader != null && reader.Read())
            {
                int documentId = Convert.ToInt32(reader["DocumentID"].ToString());
                string? documentName = reader["Name"].ToString();
                long documentSize = Convert.ToInt64(reader["Size"].ToString());
                string? documentType = reader["Type"].ToString();
                string? documentSection = reader["Section"].ToString();
                string? documentContent = reader["Content"].ToString();

                // Declare a new object of type 'CMCSDocument'.
                Document document = new()
                {
                    DocumentID = documentId,
                    Name = documentName,
                    Size = documentSize,
                    Type = documentType,
                    Section = documentSection,
                    Content = documentContent
                };

                // Check if the documentSection is 'QUALIFICATION'
                if (documentSection == "QUALIFICATION")
                {
                    // Add the document to the 'qualificationDocuments' collection.
                    qualificationDocuments.Add(document);
                }

                // Check if the documentSection is 'IDENTITY'
                if (documentSection == "IDENTITY")
                {
                    // Add the document to the 'identityDocuments' collection.
                    identityDocuments.Add(document);
                }
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();

            string sql3 = $"SELECT * FROM AccountRecovery WHERE UserID = '{CMCSMain.User.IdentityNumber}'";
            reader = await CMCSDB.RunSQLResult(sql3);

            // Variable declarations
            string? securityQuestion = string.Empty;
            bool method1Enabled = false;
            bool method2Enabled = false;

            // Iterate through the collection
            while(reader != null && reader.Read())
            {
                string? method = reader["Method"].ToString();
                string? value = reader["Value"].ToString();

                // Check if the method is 'FILE'.
                if(method == "FILE")
                {
                    method1Enabled = true;
                }

                // Check if the method is 'QUESTION'.
                if(method == "QUESTION")
                {
                    securityQuestion = value;
                    method2Enabled = true;
                }
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();

            // Instantiate a new dynamic object.
            dynamic? userAccountData = new
            {
                FirstName = firstName,
                LastName = lastName,
                IdentityNumber = identityNumber,
                EmailAddress = emailAddress,
                QualificationDocuments = qualificationDocuments.ToArray(),
                IdentityDocuments = identityDocuments.ToArray(),
                AccountRecovery = securityQuestion,
                RecoveryMethod1Enabled = method1Enabled,
                RecoveryMethod2Enabled = method2Enabled
            };

            // The request succeeded.
            this.Response.StatusCode = 1;
            this.Response.WriteAsync(JsonConvert.SerializeObject((object)userAccountData));
            this.Response.CompleteAsync();
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private void UserAccount_GetRecoveryFile()
        {
            var result = _context.AccountRecovery.Where(i => i.Method == "FILE" && i.UserID == CMCSMain.User.IdentityNumber).ToList();
            
            if(result.Count > 0)
            {
                string? value = result[0].Value;

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync($"UserID={CMCSMain.User.IdentityNumber};RecoveryKey={value}");
                this.Response.CompleteAsync();
            }
        }

        /// <summary>
        /// This method is part of the 'UserAccount' method.
        /// </summary>
        private async Task UserAccount_GetSecurityQuestion()
        {
            // Get the user id from the request header 'UserID'.
            string? userId = this.Request.Headers["UserID"];
            var result = await _context.AccountRecovery.Where(i => i.Method == "QUESTION" && i.UserID == userId).ToListAsync();

            if(result.Count > 0)
            {
                string? value = result[0].Value;
                string securityQuestion = value.Split(";")[0];

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync(securityQuestion);
                this.Response.CompleteAsync();
            }
        }

        /// <summary>
        /// This method registers a new user with CMCS.
        /// </summary>
        /// <param name="bLecturer">Indicates if the new user is a Lecturer or Manager.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="identityNumber">The user's identity number.</param>
        /// <param name="emailAddress">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="qualificationDocuments">The user's qualification documents.</param>
        /// <param name="identityDocuments">The user's identity documents.</param>
        private async Task RegisterUser(bool bLecturer, string firstName, string lastName, string identityNumber, string emailAddress, string password, Models.Document[] qualificationDocuments, Models.Document[] identityDocuments)
        {
            // Check if the user type is a Lecturer.
            if (bLecturer)
            {
                // Run a SQL query.
                await CMCSDB.RunSQLNoResult($"INSERT INTO Lecturer(FirstName, LastName, IdentityNumber, EmailAddress, Password) VALUES ('{firstName}', '{lastName}', '{identityNumber}', '{emailAddress}', '{password}')");

                // Iterate through the 'qualificationDocuments' array.
                for (int i = 0; i < qualificationDocuments.Length; i++)
                {
                    var document = qualificationDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'QUALIFICATION', '{identityNumber}', '{document.Content}')");
                }

                // Iterate through the 'identityDocuments' array.
                for (int i = 0; i < identityDocuments.Length; i++)
                {
                    var document = identityDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'IDENTITY', '{identityNumber}', '{document.Content}')");
                }
            }
            else
            {
                // Run a SQL query.
                await CMCSDB.RunSQLNoResult($"INSERT INTO Manager(FirstName, LastName, IdentityNumber, EmailAddress, Password) VALUES ('{firstName}', '{lastName}', '{identityNumber}', '{emailAddress}', '{password}')");

                // Iterate through the 'qualifiationDocuments' array.
                for (int i = 0; i < qualificationDocuments.Length; i++)
                {
                    var document = identityDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'QUALIFICATION', '{identityNumber}', '{document.Content}')");
                }

                // Iterate through the 'identityDocuments' array.
                for (int i = 0; i < identityDocuments.Length; i++)
                {
                    var document = identityDocuments[i];
                    string documentType = CMCSMain.GetDocumentType(document.Name);

                    // Run a SQL query.
                    await CMCSDB.RunSQLNoResult($"INSERT INTO Document (Name, Type, Size, Section, UserID, Content) VALUES ('{document.Name}', '{documentType}', {document.Size}, 'IDENTITY', '{identityNumber}', '{document.Content}')");
                }
            }
        }
    }
}
