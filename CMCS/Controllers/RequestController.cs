using CMCS.DBContext;
using CMCS.Models;
using Humanizer.DateTimeHumanizeStrategy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Security.Cryptography.Xml;
using System.Text;

namespace CMCS.Controllers
{
    public class RequestController : Controller
    {
        // Data fields
        private readonly AppDbContext _context;

        /// <summary>
        /// Master constructor
        /// </summary>
        /// <param name="context"></param>
        public RequestController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// CMCS Status page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Status()
        {
            return View();
        }

        /// <summary>
        /// CMCS NewRequest page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> NewRequest()
        {
            // POST method
            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "NewRequest")
                    await POST_NewRequest_New();

                if (this.Request.Headers["ActionName"] == "EditRequest")
                    await POST_NewRequest_Edit();
            }

            // GET method
            if (this.Request.Method == "GET")
            {
                if (this.Request.Query["ActionName"] == "EditRequest")
                    return await GET_NewRequest_Edit();
            }

            return View();
        }

        /// <summary>
        /// CMCS TrackRequest page.
        /// </summary>
        /// <returns></returns>
        public IActionResult TrackRequest()
        {
            return View();
        }

        /// <summary>
        /// CMCS ManageRequests page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ManageRequests()
        {
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // POST method
            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "SetRequestIndex")
                    await ManageRequests_SetRequestIndex();

                if (this.Request.Headers["ActionName"] == "AcceptRequest")
                    await ManageRequests_AcceptRequest();

                if (this.Request.Headers["ActionName"] == "RejectRequest")
                    await ManageRequests_RejectRequest();
            }

            // GET method
            if (this.Request.Method == "GET")
            {
                if (this.Request.Headers["ActionName"] == "GetRequestData")
                    await ManageRequests_GetRequestData();

                if (this.Request.Headers["ActionName"] == "GetDocumentContent")
                    await ManageRequests_GetDocumentContent();

                if (this.Request.Headers["ActionName"] == "GetDocumentContent2")
                    ManageRequests_GetDocumentContent2();

                if (this.Request.Headers["ActionName"] == "GetRequestID")
                    await ManageRequests_GetRequestID();

                if(!this.Request.Headers.ContainsKey("ActionName"))
                {
                    return await GET_ManageRequests();
                }
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();

            return View(new List<Request>());
        }

        /// <summary>
        /// CMCS CancelRequest page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CancelRequest()
        {
            // POST method
            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "CancelRequest")
                    return await POST_CancelRequest();
            }

            if (!CMCSMain.User.IsManager)
            {
                // Get the request id.
                string? requestID = this.Request.Query["RequestID"].ToString();

                // Open the database connection.
                await CMCSDB.OpenConnection();

                // Find the lecturer and get the id.
                int lecturerID = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);

                // Select all the columns where the request id is provided.
                string sql = $"SELECT * FROM Request WHERE RequestID = {requestID}";
                // Execute the sql query and instantiate a SqlDataReader? object.
                SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

                if (reader != null)
                {
                    // Check if the reader can read data.
                    if (reader.Read())
                    {
                        // Obtain the lecturer id.
                        int lecturerID2 = Convert.ToInt32(reader["LecturerID"].ToString());

                        // Check if the two values matches.
                        if (lecturerID == lecturerID2)
                        {
                            // Close the SqlDataReader? object.
                            await CMCSDB.CloseReader();
                            // Close the database connection.
                            await CMCSDB.CloseConnection();

                            return View();
                        }
                    }

                    // Close the SqlDataReader? object.
                    await CMCSDB.CloseReader();
                    // Close the database connection.
                    await CMCSDB.CloseConnection();
                }
            }

            // These values will be shown in the address bar.
            RouteValueDictionary routeValues = new RouteValueDictionary();
            routeValues.Add("ActionName", "CancelRequest");
            routeValues.Add("Status", "Failed");

            return RedirectToAction("Status", "Home", routeValues);
        }

        /// <summary>
        /// Gets the request status.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task GetRequestStatus()
        {
            // Obtain the request id from the request query.
            string? requestId = this.Request.Query["RequestID"];

            // Open the database connection.
            await CMCSDB.OpenConnection();

            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            // Declare and instantiate a SqlDataReader? object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            if (reader != null && reader.Read())
            {
                // Get the status of the request.
                string? status = reader["RequestStatus"].ToString();

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync(status);
                this.Response.CompleteAsync();
            }
            else
            {
                this.Response.StatusCode = 2;
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        /// <summary>
        /// Exports the user's requests to a log file.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ExportLog()
        {
            // Declare and instantiate a StringBuilder object.
            StringBuilder sb = new StringBuilder();

            // Obtain the user information.
            string? firstName = CMCSMain.User.FirstName;
            string? lastName = CMCSMain.User.LastName;
            string? identityNumber = CMCSMain.User.IdentityNumber;
            string creationDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Build the string output.
            sb.AppendLine($"LOG REPORT");
            sb.AppendLine($"User: {firstName} {lastName}");
            sb.AppendLine($"Identity Number: {identityNumber}");
            sb.AppendLine($"Date Created: {creationDate}");
            sb.AppendLine("-------------------------------------");
            sb.AppendLine();

            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Obtain the lecturer id.
            int lecturerId = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
            string sql = $"SELECT * FROM Request r INNER JOIN RequestProcess p ON r.RequestID = p.RequestID WHERE LecturerID = {lecturerId}";
            // Declare and instantiate a SqlDataReader? object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql);

            if (reader != null)
            {
                sb.AppendLine("Accepted/Rejected Request(s):");

                // Iterate while the SqlDataReader? object can read data.
                while (reader.Read())
                {
                    // Obtain the data from the SqlDataReader? object.
                    string requestID = reader["RequestID"].ToString();
                    string requestFor = reader["RequestFor"].ToString();
                    string hoursWorked = reader["HoursWorked"].ToString();
                    string hourlyRate = reader["HourlyRate"].ToString();
                    string description = reader["Description"].ToString();
                    string requestStatus = reader["RequestStatus"].ToString();
                    string dateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString()).ToString("dd/MM/yyyy HH:mm:ss");
                    string date = DateTime.Parse(reader["Date"].ToString()).ToString("dd/MM/yyyy HH:mm:ss");

                    sb.AppendLine($"Request ID: {requestID}; Request For: {requestFor}; Hours Worked: {hoursWorked}; Hourly Rate: {hourlyRate}; Description: {description}; Date Submitted: {dateSubmitted}; Date Approved/Rejected: {date}");
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();

                string sql2 = $"SELECT * FROM Request WHERE LecturerID = {lecturerId} AND RequestStatus = 'Pending'";
                // Re-instantiate a SqlDataReader? object.
                reader = await CMCSDB.RunSQLResult(sql2);

                sb.AppendLine();
                sb.AppendLine("Pending Request(s):");

                // Iterate while the SqlDataReader? object can read data.
                while (reader.Read())
                {
                    // Obtain the data from the SqlDataReader? object.
                    string requestID = reader["RequestID"].ToString();
                    string requestFor = reader["RequestFor"].ToString();
                    string hoursWorked = reader["HoursWorked"].ToString();
                    string hourlyRate = reader["HourlyRate"].ToString();
                    string description = reader["Description"].ToString();
                    string requestStatus = reader["RequestStatus"].ToString();
                    string dateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString()).ToString("dd/MM/yyyy HH:mm:ss");

                    sb.AppendLine($"Request ID: {requestID}; Request For: {requestFor}; Hours Worked: {hoursWorked}; Hourly Rate: {hourlyRate}; Description: {description}; Date Submitted: {dateSubmitted}");
                }


                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.Headers.Add("FileName", $"LOG-{Guid.NewGuid().ToString().ToUpper()}.txt");
                this.Response.WriteAsync(sb.ToString());
                this.Response.CompleteAsync();
            }
            else
            {
                this.Response.StatusCode = 2;
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
            // Close the database connection.
            await CMCSDB.CloseConnection();

            return View();
        }

        /// <summary>
        /// This method is part of the NewRequest method.
        /// </summary>
        private async Task POST_NewRequest_New()
        {
            // Read the content from the request body, asynchronously.
            var sRequestData = await new StreamReader(this.Request.Body).ReadToEndAsync();
            // Parse the string content into a dynamic object, using JsonConvert.
            dynamic? requestData = JsonConvert.DeserializeObject(sRequestData);

            // Get the fields from the dynamic object as strings.
            string? requestFor = Convert.ToString(requestData?.RequestFor);
            string? hoursWorked = Convert.ToString(requestData?.HoursWorked);
            string? hourlyRate = Convert.ToString(requestData?.HourlyRate);
            string? description = Convert.ToString(requestData?.Description);

            dynamic? documents = requestData?.Documents;

            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Get the lecturerId from an IdentityNumber.
            int lecturerId = await CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);

            string sql2 = $"INSERT INTO Request(LecturerID, RequestFor, HoursWorked, HourlyRate, Description, RequestStatus, DateSubmitted) OUTPUT INSERTED.RequestID VALUES ('{lecturerId}', '{requestFor}', {hoursWorked}, {hourlyRate}, '{description}', 'Pending', '{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}')";
            // Run the sql query and obtain the output.
            int? requestID = (int?)await CMCSDB.RunSQLResultScalar(sql2);

            // Iterate through the 'documents' collection.
            for (int i = 0; i < documents?.Count; i++)
            {
                // Get the field values from the dynamic object 'dpcuments'.
                string documentName = Convert.ToString(documents[i]?.Name);
                string documentType = CMCSMain.GetDocumentType(documentName);

                string sql1 = $"INSERT INTO Document(Name, Type, Size, Section, UserID, Content) OUTPUT INSERTED.DocumentID VALUES ('{documents?[i].Name}', '{documentType}', {documents?[i].Size}, 'REQUEST', '{CMCSMain.User.IdentityNumber}', '{documents?[i].Content}')";
                // Run the sql query and obtain the output.
                int documentID = (int)await CMCSDB.RunSQLResultScalar(sql1);

                string sql3 = $"INSERT INTO RequestDocument(RequestID, DocumentID) VALUES ({requestID}, {documentID})";
                // Run the sql query and do not return an output value.
                await CMCSDB.RunSQLNoResult(sql3);
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        /// <summary>
        /// This method is part of the CancelRequest method.
        /// </summary>
        /// <returns></returns>
        private async Task<IActionResult> POST_CancelRequest()
        {
            // Obtain the request id from the request header collection.
            string? requestId = this.Request.Headers["RequestID"].ToString();

            // Open the database connection.
            await CMCSDB.OpenConnection();

            string sql4 = $"SELECT * FROM RequestDocument WHERE RequestID = {requestId}";
            // Create a SqlDataReader? object from a sql query.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql4);

            if (reader != null)
            {
                // Declare and instantiate a List<int> object.
                List<int> documentList = new List<int>();

                // Iterate through the collection.
                while (reader.Read())
                {
                    int documentId = Convert.ToInt32(reader["DocumentID"].ToString());
                    documentList.Add(documentId);
                }

                // Close the SqlDataReader?.
                await CMCSDB.CloseReader();

                // Iterate through the collection.
                foreach (int documentId in documentList)
                {
                    string sql5 = $"DELETE FROM Document WHERE DocumentID = {documentId}";
                    await CMCSDB.RunSQLNoResult(sql5);
                }

                // Delete all the data from the Request, RequestDocument and RequestProcess tables.
                string sql1 = $"DELETE FROM Request WHERE RequestID = {requestId}";
                string sql2 = $"DELETE FROM RequestDocument WHERE RequestID = {requestId}";
                string sql3 = $"DELETE FROM RequestProcess WHERE RequestID = {requestId}";

                // Run the sql queries.
                await CMCSDB.RunSQLNoResult(sql1);
                await CMCSDB.RunSQLNoResult(sql2);
                await CMCSDB.RunSQLNoResult(sql3);
                // The request succeeded.
                this.Response.StatusCode = 1;
            }
            else
            {
                this.Response.StatusCode = 2;
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();

            return View();
        }

        /// <summary>
        /// This method is part of the NewRequest method.
        /// </summary>
        private async Task POST_NewRequest_Edit()
        {
            // Obtain the request id from the request query.
            string? sRequestID = this.Request.Headers["RequestID"];

            // Read the data from the request body, asynchronously.
            var sRequestData = await new StreamReader(this.Request.Body).ReadToEndAsync();
            // Convert the string to a dynamic object using JsonConvert.
            dynamic? requestData = JsonConvert.DeserializeObject(sRequestData);

            // Obtain the field values from the dynamic object 'requestData'.
            string? requestFor = Convert.ToString(requestData?.RequestFor);
            string? hoursWorked = Convert.ToString(requestData?.HoursWorked);
            string? hourlyRate = Convert.ToString(requestData?.HourlyRate);
            string? description = Convert.ToString(requestData?.Description);

            dynamic? documents = requestData?.Documents;
            dynamic? deletedDocuments = requestData?.DeletedDocuments;

            // Open the database connection.
            await CMCSDB.OpenConnection();

            string sql2 = $"UPDATE Request SET RequestFor = '{requestFor}', HoursWorked = {hoursWorked}, HourlyRate = {hourlyRate}, Description = '{description}' WHERE RequestID = {sRequestID}";
            await CMCSDB.RunSQLNoResult(sql2);

            // Iterate through the collection.
            for (int i = 0; i < documents?.Count; i++)
            {
                string documentName = Convert.ToString(documents[i]?.Name);
                string documentType = CMCSMain.GetDocumentType(documentName);

                string sql1 = $"INSERT INTO Document(Name, Type, Size, Section, UserID, Content) OUTPUT INSERTED.DocumentID VALUES ('{documents?[i].Name}', '{documentType}', {documents?[i].Size}, 'REQUEST', '{CMCSMain.User.IdentityNumber}', '{documents?[i].Content}')";
                int? documentID = (int?)await CMCSDB.RunSQLResultScalar(sql1);

                string sql3 = $"INSERT INTO RequestDocument(RequestID, DocumentID) VALUES ({sRequestID}, {documentID})";
                await CMCSDB.RunSQLNoResult(sql3);
            }

            // Check if the variable 'deletedDocuments' does not equal 'DELETE_ALL'.
            if (Convert.ToString(deletedDocuments) != "DELETE_ALL")
            {
                // Iterate through the collection.
                for (int i = 0; i < deletedDocuments?.Count; i++)
                {
                    int documentId = Convert.ToInt32(deletedDocuments[i]);

                    string sql1 = $"DELETE FROM RequestDocument WHERE DocumentID = {documentId}";
                    await CMCSDB.RunSQLNoResult(sql1);

                    string sql3 = $"DELETE FROM Document WHERE DocumentID = {documentId}";
                    await CMCSDB.RunSQLNoResult(sql3);
                }

                // The request succeeded.
                this.Response.StatusCode = 1;
            }
            else
            {
                // Variable declarations.
                string sql = $"SELECT * FROM RequestDocument WHERE RequestID = {sRequestID}";
                int row_count = await CMCSDB.CountRows(sql);
                var reader = await CMCSDB.RunSQLResult(sql);

                int[] documentIDList = new int[row_count];

                if (reader != null)
                {
                    // Iterate through the collection.
                    for (int i = 0; i < row_count; i++)
                    {
                        reader.Read();
                        documentIDList[i] = Convert.ToInt32(reader["DocumentID"].ToString());
                    }
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();

                // Iterate through the collection.
                for (int i = 0; i < documentIDList.Length; i++)
                {
                    string sql3 = $"DELETE FROM Document WHERE DocumentID = {documentIDList[i]}";
                    await CMCSDB.RunSQLNoResult(sql3);
                }

                string sql4 = $"DELETE FROM RequestDocument WHERE RequestID = {sRequestID}";
                await CMCSDB.RunSQLNoResult(sql4);

                if(reader != null)
                {
                    // The request succeeded.
                    this.Response.StatusCode = 1;
                }
                else
                {
                    this.Response.StatusCode = 2;
                }
            }

            // Close the database connection.
            await CMCSDB.CloseConnection();
        }

        /// <summary>
        /// This method is part of the NewRequest method.
        /// </summary>
        /// <returns></returns>
        private async Task<IActionResult> GET_NewRequest_Edit()
        {
            // Open the database connection.
            await CMCSDB.OpenConnection();

            // Get the request id from the request query.
            string? sRequestID = this.Request.Query["RequestID"];

            string sql4 = $"SELECT * FROM Request WHERE RequestID = {sRequestID}";
            // Declare and instantiate a SqlDataReader? object.
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql4);
            reader.Read();

            string? sRequestFor = reader["RequestFor"].ToString();
            string? sHoursWorked = reader["HoursWorked"].ToString();
            string? sHourlyRate = reader["HourlyRate"].ToString();
            string? sDescription = reader["Description"].ToString();

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();

            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {sRequestID}";
            int row_count = await CMCSDB.CountRows(sql);
            reader = await CMCSDB.RunSQLResult(sql);

            // Declare and instantiate a List<CMCSDocument> object.
            List<Document> documents = new List<Document>();

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < row_count; i++)
                {
                    reader.Read();

                    documents.Add(new Document
                    {
                        DocumentID = Convert.ToInt32(reader["DocumentID"].ToString()),
                        Name = reader["Name"].ToString(),
                        Size = Convert.ToInt32(reader["Size"].ToString()),
                        Type = reader["Type"].ToString(),
                        Section = reader["Section"].ToString(),
                        Content = reader["Content"].ToString()
                    });
                }
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();
            // Close the database connection.
            await CMCSDB.CloseConnection();

            var view = View(documents);
            view.ViewData["RequestID"] = sRequestID;
            view.ViewData["RequestFor"] = sRequestFor;
            view.ViewData["HoursWorked"] = sHoursWorked;
            view.ViewData["HourlyRate"] = sHourlyRate;
            view.ViewData["Description"] = sDescription;

            return view;
        }

        /// <summary>
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task ManageRequests_SetRequestIndex()
        {
            // Set the selected request index.
            CMCSMain.SelectedRequestIndex = Convert.ToInt32(this.Request.Headers["RowSelected"]);

            var reader = await CMCSDB.RunSQLResult($"SELECT * FROM Request");

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
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task ManageRequests_GetRequestID()
        {
            // Obtain the request index from the request header 'RequestIndex'.
            int requestIndex = Convert.ToInt32(this.Request.Headers["RequestIndex"]);
            var reader = await CMCSDB.RunSQLResult("SELECT * FROM Request");

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < requestIndex; i++)
                    reader.Read();

                // Read data from the SqlDataReader? object.
                reader.Read();
                int requestID = Convert.ToInt32(reader["RequestID"].ToString());

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync(requestID.ToString());
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
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task ManageRequests_AcceptRequest()
        {
            // Get the request id from the request header 'RequestID'.
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"].ToString());

            string sql = $"UPDATE Request SET RequestStatus = 'Approved' WHERE RequestID = {requestID}";
            await CMCSDB.RunSQLNoResult(sql);

            int managerID = (await CMCSDB.FindManager(CMCSMain.User.IdentityNumber));
            string sql2 = $"INSERT INTO RequestProcess(RequestID, ManagerID, Date, Status) VALUES ({requestID}, {managerID}, '{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}', 'Approved')";
            await CMCSDB.RunSQLNoResult(sql2);

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        /// <summary>
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task ManageRequests_RejectRequest()
        {
            // Get the request id from the request header 'RequestID'.
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"].ToString());

            string sql = $"UPDATE Request SET RequestStatus = 'Rejected' WHERE RequestID = {requestID}";
            await CMCSDB.RunSQLNoResult(sql);

            int managerID = await CMCSDB.FindManager(CMCSMain.User.IdentityNumber);
            string sql2 = $"INSERT INTO RequestProcess(RequestID, ManagerID, Date, Status) VALUES ({requestID}, {managerID}, '{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}', 'Rejected')";
            await CMCSDB.RunSQLNoResult(sql2);

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        /// <summary>
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task ManageRequests_GetRequestData()
        {
            // Get the request id from the request header 'RequestID'.
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"]);
            string sql2 = $"SELECT * FROM Request r INNER JOIN Lecturer l ON r.LecturerID = l.LecturerID WHERE RequestID = {requestID}";
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql2);

            if (reader != null)
            {
                // Check if the SqlDataReader? object can read data.
                if (reader.Read())
                {
                    // Obtain the first name and last name from the SqlDataReader? object.
                    string firstName = reader["FirstName"].ToString();
                    string lastName = reader["LastName"].ToString();

                    // Add new response headers to carry the information.
                    this.Response.Headers.Add("RequestID", requestID.ToString());
                    this.Response.Headers.Add("LecturerID", reader["LecturerID"].ToString());
                    this.Response.Headers.Add("IdentityNumber", reader["IdentityNumber"].ToString());
                    this.Response.Headers.Add("Lecturer", $"{firstName} {lastName}");
                    this.Response.Headers.Add("RequestFor", reader["RequestFor"].ToString());
                    this.Response.Headers.Add("HoursWorked", reader["HoursWorked"].ToString());
                    this.Response.Headers.Add("HourlyRate", reader["HourlyRate"].ToString());
                    this.Response.Headers.Add("Description", reader["Description"].ToString());
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();

                string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
                int row_count = await CMCSDB.CountRows(sql);
                reader = await CMCSDB.RunSQLResult(sql);

                // Declare and instantiate a List<CMCSDocument> object.
                List<Document> documentList = new List<Document>();

                if (reader != null)
                {
                    // Iterate through the collection.
                    for (int i = 0; i < row_count; i++)
                    {
                        // Read data from the SqlDataReader? object.
                        reader.Read();

                        var document = new Document();
                        document.DocumentID = Convert.ToInt32(reader["DocumentID"]);
                        document.Name = reader["Name"].ToString();
                        document.Size = Convert.ToInt32(reader["Size"]);
                        document.Type = reader["Type"].ToString();
                        document.Section = reader["Section"].ToString();
                        document.Content = reader["Content"].ToString();

                        documentList.Add(document);
                    }
                }

                // Close the SqlDataReader? object.
                await CMCSDB.CloseReader();

                // The request succeeded.
                this.Response.StatusCode = 1;
                this.Response.WriteAsync(JsonConvert.SerializeObject(documentList.ToArray()));
                this.Response.CompleteAsync();
            }
            else
            {
                this.Response.StatusCode = 2;
            }
        }

        /// <summary>
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task ManageRequests_GetDocumentContent()
        {
            // Obtain the data from the request headers 'RequestID' and 'FileIndex'.
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

        private void ManageRequests_GetDocumentContent2()
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
        /// This method is part of the ManageRequests method.
        /// </summary>
        private async Task<IActionResult> GET_ManageRequests()
        {
            string sql1 = $"SELECT * FROM Request";
            int row_count = await CMCSDB.CountRows(sql1);
            SqlDataReader? reader = await CMCSDB.RunSQLResult(sql1);

            // Declare and instantiate a List<CMCDRequest> object.
            List<Request> requestList = new List<Request>();

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < row_count; i++)
                {
                    // Read data from the SqlDataReader? object.
                    reader.Read();

                    // Declare and instantiate a CMCSRequest object.
                    var request = new Request();
                    request.RequestID = Convert.ToInt32(reader["RequestID"].ToString());
                    request.LecturerID = Convert.ToInt32(reader["LecturerID"].ToString());
                    request.RequestFor = reader["RequestFor"].ToString();
                    request.HoursWorked = Convert.ToInt32(reader["HoursWorked"].ToString());
                    request.HourlyRate = Convert.ToInt32(reader["HourlyRate"].ToString());
                    request.Description = reader["Description"].ToString();
                    request.DateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString());
                    request.RequestStatus = reader["RequestStatus"].ToString();

                    // Add the request object to the collection 'requestList'.
                    requestList.Add(request);
                }
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();

            string sql2 = $"SELECT * FROM Request r INNER JOIN RequestProcess p ON r.RequestID = p.RequestID";
            int row_count2 = await CMCSDB.CountRows(sql2);
            reader = await CMCSDB.RunSQLResult(sql2);

            if (reader != null)
            {
                // Iterate through the collection.
                for (int i = 0; i < row_count2; i++)
                {
                    // Read data from the SqlDataReader? object.
                    reader.Read();

                    // Get the request id from the reader.
                    int requestID = Convert.ToInt32(reader["RequestID"].ToString());

                    // Inner For Loop: Iterate through the requetList collection.
                    for (int j = 0; j < requestList.Count; j++)
                    {
                        // Check if there is a match.
                        if (requestList[j].RequestID == requestID)
                        {
                            requestList[j].DateApproved = DateTime.Parse(reader["Date"].ToString());
                            requestList[j].RequestStatus = reader["Status"].ToString();
                        }
                    }
                }
            }

            // Close the SqlDataReader? object.
            await CMCSDB.CloseReader();

            // Declare and instantiate a generic collection.
            List<string> lecturerNames = new List<string>();

            // Iterate through the collection.
            for (int i = 0; i < requestList.Count; i++)
            {
                string sql3 = $"SELECT * FROM Lecturer WHERE LecturerID = {requestList[i].LecturerID}";
                reader = await CMCSDB.RunSQLResult(sql3);

                if (reader != null && reader.Read())
                {
                    string lecturerName = $"{reader["FirstName"]} {reader["LastName"]}";

                    // Add the result to the 'lecturerNames' collection.
                    lecturerNames.Add(lecturerName);
                }

                // Close the reader.
                await CMCSDB.CloseReader();
            }

            var view = View(requestList);
            view.ViewData["LecturerNames"] = lecturerNames.ToArray();

            return view;
        }
    }
}
