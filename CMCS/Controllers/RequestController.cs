using CMCS.Models;
using Humanizer.DateTimeHumanizeStrategy;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Status()
        {
            return View();
        }

        [HttpGet]
        [HttpPost]
        public IActionResult NewRequest()
        {
            if (this.Request.Method == "POST")
            {
                if (this.Request.Query["ActionName"] == "NewRequest")
                {
                    var sRequestData = new StreamReader(this.Request.Body).ReadToEndAsync().Result;
                    dynamic? requestData = JsonConvert.DeserializeObject(sRequestData);

                    string? requestFor = Convert.ToString(requestData?.RequestFor);
                    string? hoursWorked = Convert.ToString(requestData?.HoursWorked);
                    string? hourlyRate = Convert.ToString(requestData?.HourlyRate);
                    string? description = Convert.ToString(requestData?.Description);

                    dynamic? documents = requestData?.Documents;
                    CMCSDB.OpenConnection();

                    int lecturerId = CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);

                    string sql2 = $"INSERT INTO Request(LecturerID, RequestFor, HoursWorked, HourlyRate, Description, RequestStatus, DateSubmitted) OUTPUT INSERTED.RequestID VALUES ('{lecturerId}', '{requestFor}', {hoursWorked}, {hourlyRate}, '{description}', 'Pending', '{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}')";
                    int requestID = (int)CMCSDB.RunSQLResultScalar(sql2);

                    for (int i = 0; i < documents?.Count; i++)
                    {
                        string documentName = Convert.ToString(documents[i]?.Name);
                        string documentType = CMCSMain.GetDocumentType(documentName);

                        string sql1 = $"INSERT INTO Document(Name, Type, Size, Section, UserID, Content) OUTPUT INSERTED.DocumentID VALUES ('{documents?[i].Name}', '{documentType}', {documents?[i].Size}, 'REQUEST', '{CMCSMain.User.IdentityNumber}', '{documents?[i].Content}')";
                        int documentID = (int)CMCSDB.RunSQLResultScalar(sql1);

                        string sql3 = $"INSERT INTO RequestDocument(RequestID, DocumentID) VALUES ({requestID}, {documentID})";
                        CMCSDB.RunSQLNoResult(sql3);
                    }

                    CMCSDB.CloseConnection();

                    this.Response.StatusCode = 1;
                }

                if (this.Request.Query["ActionName"] == "EditRequest")
                {
                    string? sRequestID = this.Request.Query["RequestID"];

                    var sRequestData = new StreamReader(this.Request.Body).ReadToEndAsync().Result;
                    dynamic? requestData = JsonConvert.DeserializeObject(sRequestData);

                    string? requestFor = Convert.ToString(requestData?.RequestFor);
                    string? hoursWorked = Convert.ToString(requestData?.HoursWorked);
                    string? hourlyRate = Convert.ToString(requestData?.HourlyRate);
                    string? description = Convert.ToString(requestData?.Description);

                    dynamic? documents = requestData?.Documents;
                    dynamic? deletedDocuments = requestData?.DeletedDocuments;

                    CMCSDB.OpenConnection();

                    string sql2 = $"UPDATE Request SET RequestFor = '{requestFor}', HoursWorked = {hoursWorked}, HourlyRate = {hourlyRate}, Description = '{description}' WHERE RequestID = {sRequestID}";
                    CMCSDB.RunSQLNoResult(sql2);

                    for (int i = 0; i < documents?.Count; i++)
                    {
                        string documentName = Convert.ToString(documents[i]?.Name);
                        string documentType = CMCSMain.GetDocumentType(documentName);

                        string sql1 = $"INSERT INTO Document(Name, Type, Size, Section, UserID, Content) OUTPUT INSERTED.DocumentID VALUES ('{documents?[i].Name}', '{documentType}', {documents?[i].Size}, 'REQUEST', '{CMCSMain.User.IdentityNumber}', '{documents?[i].Content}')";
                        int documentID = (int)CMCSDB.RunSQLResultScalar(sql1);

                        string sql3 = $"INSERT INTO RequestDocument(RequestID, DocumentID) VALUES ({sRequestID}, {documentID})";
                        CMCSDB.RunSQLResult(sql3);
                    }

                    if (Convert.ToString(deletedDocuments) != "DELETE_ALL")
                    {
                        for (int i = 0; i < deletedDocuments?.Count; i++)
                        {
                            int documentId = Convert.ToInt32(deletedDocuments[i]);

                            string sql1 = $"DELETE FROM RequestDocument WHERE DocumentID = {documentId}";
                            CMCSDB.RunSQLNoResult(sql1);

                            string sql3 = $"DELETE FROM Document WHERE DocumentID = {documentId}";
                            CMCSDB.RunSQLNoResult(sql3);
                        }
                    }
                    else
                    {
                        string sql = $"SELECT * FROM RequestDocument WHERE RequestID = {sRequestID}";
                        int row_count = CMCSDB.CountRows(sql);
                        var reader = CMCSDB.RunSQLResult(sql);

                        int[] documentIDList = new int[row_count];

                        for (int i = 0; i < row_count; i++)
                        {
                            reader.Read();
                            documentIDList[i] = Convert.ToInt32(reader["DocumentID"].ToString());
                        }

                        reader.Close();

                        for (int i = 0; i < documentIDList.Length; i++)
                        {
                            string sql3 = $"DELETE FROM Document WHERE DocumentID = {documentIDList[i]}";
                            CMCSDB.RunSQLNoResult(sql3);
                        }

                        string sql4 = $"DELETE FROM RequestDocument WHERE RequestID = {sRequestID}";
                        CMCSDB.RunSQLNoResult(sql4);
                    }

                    CMCSDB.CloseConnection();

                    this.Response.StatusCode = 1;
                }
            }

            if (this.Request.Method == "GET")
            {
                if (this.Request.Query["ActionName"] == "EditRequest")
                {
                    CMCSDB.OpenConnection();

                    string? sRequestID = this.Request.Query["RequestID"];

                    string sql4 = $"SELECT * FROM Request WHERE RequestID = {sRequestID}";
                    SqlDataReader reader = CMCSDB.RunSQLResult(sql4);
                    reader.Read();

                    string? sRequestFor = reader["RequestFor"].ToString();
                    string? sHoursWorked = reader["HoursWorked"].ToString();
                    string? sHourlyRate = reader["HourlyRate"].ToString();
                    string? sDescription = reader["Description"].ToString();

                    reader.Close();

                    string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {sRequestID}";
                    int row_count = CMCSDB.CountRows(sql);
                    reader = CMCSDB.RunSQLResult(sql);

                    List<CMCSDocument> documents = new List<CMCSDocument>();

                    for (int i = 0; i < row_count; i++)
                    {
                        reader.Read();

                        documents.Add(new CMCSDocument
                        {
                            DocumentID = Convert.ToInt32(reader["DocumentID"].ToString()),
                            Name = reader["Name"].ToString(),
                            Size = Convert.ToInt32(reader["Size"].ToString()),
                            Type = reader["Type"].ToString(),
                            Section = reader["Section"].ToString(),
                            Content = reader["Content"].ToString()
                        });
                    }

                    reader.Close();
                    CMCSDB.CloseConnection();

                    var view = View(documents);
                    view.ViewData["RequestID"] = sRequestID;
                    view.ViewData["RequestFor"] = sRequestFor;
                    view.ViewData["HoursWorked"] = sHoursWorked;
                    view.ViewData["HourlyRate"] = sHourlyRate;
                    view.ViewData["Description"] = sDescription;

                    return view;
                }
            }

            return View();
        }

        public IActionResult TrackRequest()
        {
            return View();
        }

        private void ManageRequests_SetRequestIndex()
        {
            CMCSMain.SelectedRequestIndex = Convert.ToInt32(this.Request.Headers["RowSelected"]);

            var reader = CMCSDB.RunSQLResult($"SELECT * FROM Request");

            for (int i = 0; i < CMCSMain.SelectedRequestIndex; i++)
                reader.Read();

            reader.Read();
            CMCSMain.SelectedRequestID = Convert.ToInt32(reader["RequestID"].ToString());
            reader.Close();

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        private void ManageRequests_GetRequestID()
        {
            int requestIndex = Convert.ToInt32(this.Request.Headers["RequestIndex"]);
            var reader = CMCSDB.RunSQLResult("SELECT * FROM Request");

            for (int i = 0; i < requestIndex; i++)
                reader.Read();

            reader.Read();
            int requestID = Convert.ToInt32(reader["RequestID"].ToString());
            reader.Close();

            this.Response.WriteAsync(requestID.ToString());
            this.Response.CompleteAsync();
        }

        private void ManageRequests_AcceptRequest()
        {
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"].ToString());

            string sql = $"UPDATE Request SET RequestStatus = 'Approved' WHERE RequestID = {requestID}";
            CMCSDB.RunSQLNoResult(sql);

            int managerID = CMCSDB.FindManager(CMCSMain.User.IdentityNumber);
            string sql2 = $"INSERT INTO RequestProcess(RequestID, ManagerID, Date, Status) VALUES ({requestID}, {managerID}, '{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}', 'Approved')";
            CMCSDB.RunSQLNoResult(sql2);

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        private void ManageRequests_RejectRequest()
        {
            int requestIndex = Convert.ToInt32(this.Request.Headers["RowSelected"].ToString());
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"].ToString());

            string sql = $"UPDATE Request SET RequestStatus = 'Rejected' WHERE RequestID = {requestID}";
            CMCSDB.RunSQLNoResult(sql);

            int managerID = CMCSDB.FindManager(CMCSMain.User.IdentityNumber);
            string sql2 = $"INSERT INTO RequestProcess(RequestID, ManagerID, Date, Status) VALUES ({requestID}, {managerID}, '{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}', 'Rejected')";
            CMCSDB.RunSQLNoResult(sql2);

            // The request succeeded.
            this.Response.StatusCode = 1;
        }

        private void ManageRequests_GetRequestData()
        {
            int requestID = Convert.ToInt32(this.Request.Headers["RequestID"]);
            string sql2 = $"SELECT * FROM Request r INNER JOIN Lecturer l ON r.LecturerID = l.LecturerID WHERE RequestID = {requestID}";
            SqlDataReader reader = CMCSDB.RunSQLResult(sql2);

            if(reader.Read())
            {
                string firstName = reader["FirstName"].ToString();
                string lastName = reader["LastName"].ToString();

                this.Response.Headers.Add("RequestID", requestID.ToString());
                this.Response.Headers.Add("LecturerID", reader["LecturerID"].ToString());
                this.Response.Headers.Add("IdentityNumber", reader["IdentityNumber"].ToString());
                this.Response.Headers.Add("Lecturer", $"{firstName} {lastName}");
                this.Response.Headers.Add("RequestFor", reader["RequestFor"].ToString());
                this.Response.Headers.Add("HoursWorked", reader["HoursWorked"].ToString());
                this.Response.Headers.Add("HourlyRate", reader["HourlyRate"].ToString());
                this.Response.Headers.Add("Description", reader["Description"].ToString());
            }

            reader.Close();

            string sql = $"SELECT * FROM RequestDocument r INNER JOIN Document d ON r.DocumentID = d.DocumentID WHERE r.RequestID = {requestID}";
            int row_count = CMCSDB.CountRows(sql);
            reader = CMCSDB.RunSQLResult(sql);

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

        private void ManageRequests_GetDocumentContent()
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
        public IActionResult ManageRequests()
        {
            CMCSDB.OpenConnection();

            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "SetRequestIndex")
                    ManageRequests_SetRequestIndex();

                if (this.Request.Headers["ActionName"] == "AcceptRequest")
                    ManageRequests_AcceptRequest();

                if (this.Request.Headers["ActionName"] == "RejectRequest")
                    ManageRequests_RejectRequest();
            }

            if (this.Request.Method == "GET")
            {
                if (this.Request.Headers["ActionName"] == "GetRequestData")
                    ManageRequests_GetRequestData();

                if (this.Request.Headers["ActionName"] == "GetDocumentContent")
                    ManageRequests_GetDocumentContent();

                if (this.Request.Headers["ActionName"] == "GetRequestID")
                    ManageRequests_GetRequestID();
            }

            string sql1 = $"SELECT * FROM Request";
            int row_count = CMCSDB.CountRows(sql1);
            SqlDataReader reader = CMCSDB.RunSQLResult(sql1);

            List<CMCSRequest> requestList = new List<CMCSRequest>();

            for (int i = 0; i < row_count; i++)
            {
                reader.Read();

                var request = new CMCSRequest();
                request.RequestID = Convert.ToInt32(reader["RequestID"].ToString());
                request.LecturerID = Convert.ToInt32(reader["LecturerID"].ToString());
                request.RequestFor = reader["RequestFor"].ToString();
                request.HoursWorked = Convert.ToInt32(reader["HoursWorked"].ToString());
                request.HourlyRate = Convert.ToInt32(reader["HourlyRate"].ToString());
                request.Description = reader["Description"].ToString();
                request.DateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString());
                request.RequestStatus = reader["RequestStatus"].ToString();

                requestList.Add(request);
            }

            reader.Close();

            string sql2 = $"SELECT * FROM Request r INNER JOIN RequestProcess p ON r.RequestID = p.RequestID";
            int row_count2 = CMCSDB.CountRows(sql2);
            reader = CMCSDB.RunSQLResult(sql2);

            for (int i = 0; i < row_count2; i++)
            {
                reader.Read();

                int requestID = Convert.ToInt32(reader["RequestID"].ToString());

                for (int j = 0; j < requestList.Count; j++)
                {
                    if (requestList[j].RequestID == requestID)
                    {
                        requestList[j].DateApproved = DateTime.Parse(reader["Date"].ToString());
                        requestList[j].RequestStatus = reader["Status"].ToString();
                    }
                }
            }

            reader.Close();

            List<string> lecturerNames = new List<string>();

            for (int i = 0; i < requestList.Count; i++)
            {
                string sql3 = $"SELECT * FROM Lecturer WHERE LecturerID = {requestList[i].LecturerID}";
                reader = CMCSDB.RunSQLResult(sql3);
                reader.Read();

                string lecturerName = $"{reader["FirstName"]} {reader["LastName"]}";
                reader.Close();

                lecturerNames.Add(lecturerName);
            }

            var view = View(requestList);
            view.ViewData["LecturerNames"] = lecturerNames.ToArray();

            CMCSDB.CloseConnection();

            return view;
        }

        [HttpGet]
        [HttpPost]
        public IActionResult CancelRequest()
        {
            if (this.Request.Method == "POST")
            {
                if (this.Request.Headers["ActionName"] == "CancelRequest")
                {
                    string? requestId = this.Request.Headers["RequestID"].ToString();

                    CMCSDB.OpenConnection();

                    string sql4 = $"SELECT * FROM RequestDocument WHERE RequestID = {requestId}";
                    SqlDataReader reader = CMCSDB.RunSQLResult(sql4);

                    List<int> documentList = new List<int>();

                    while(reader.Read())
                    {
                        int documentId = Convert.ToInt32(reader["DocumentID"].ToString());
                        documentList.Add(documentId);
                    }

                    reader.Close();

                    foreach(int documentId in documentList)
                    {
                        string sql5 = $"DELETE FROM Document WHERE DocumentID = {documentId}";
                        CMCSDB.RunSQLNoResult(sql5);
                    }

                    string sql1 = $"DELETE FROM Request WHERE RequestID = {requestId}";
                    string sql2 = $"DELETE FROM RequestDocument WHERE RequestID = {requestId}";
                    string sql3 = $"DELETE FROM RequestProcess WHERE RequestID = {requestId}";

                    CMCSDB.RunSQLNoResult(sql1);
                    CMCSDB.RunSQLNoResult(sql2);
                    CMCSDB.RunSQLNoResult(sql3);

                    CMCSDB.CloseConnection();

                    this.Response.StatusCode = 1;

                    return View();
                }
            }

            if (!CMCSMain.User.IsManager)
            {
                string? requestID = this.Request.Query["RequestID"].ToString();

                CMCSDB.OpenConnection();

                int lecturerID = CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);

                string sql = $"SELECT * FROM Request WHERE RequestID = {requestID}";
                SqlDataReader reader = CMCSDB.RunSQLResult(sql);

                if (reader.Read())
                {
                    int lecturerID2 = Convert.ToInt32(reader["LecturerID"].ToString());

                    if (lecturerID == lecturerID2)
                    {
                        reader.Close();
                        CMCSDB.CloseConnection();

                        return View();
                    }
                }

                reader.Close();
                CMCSDB.CloseConnection();
            }

            RouteValueDictionary routeValues = new RouteValueDictionary();
            routeValues.Add("ActionName", "CancelRequest");
            routeValues.Add("Status", "Failed");

            return RedirectToAction("Status", "Home", routeValues);
        }

        [HttpGet]
        public IActionResult GetRequestStatus()
        {
            string? requestId = this.Request.Query["RequestID"];

            CMCSDB.OpenConnection();

            string sql = $"SELECT RequestStatus FROM Request WHERE RequestID = {requestId}";
            SqlDataReader reader = CMCSDB.RunSQLResult(sql);

            if(reader.Read())
            {
                string? status = reader["RequestStatus"].ToString();

                this.Response.WriteAsync(status);
                this.Response.CompleteAsync();
            }

            reader.Close();
            CMCSDB.CloseConnection();

            return View();
        }

        [HttpGet]
        public IActionResult ExportLog()
        {
            StringBuilder sb = new StringBuilder();

            string? firstName = CMCSMain.User.FirstName;
            string? lastName = CMCSMain.User.LastName;
            string? identityNumber = CMCSMain.User.IdentityNumber;
            string creationDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            sb.AppendLine($"LOG REPORT");
            sb.AppendLine($"User: {firstName} {lastName}");
            sb.AppendLine($"Identity Number: {identityNumber}");
            sb.AppendLine($"Date Created: {creationDate}");
            sb.AppendLine("-------------------------------------");
            sb.AppendLine();

            CMCSDB.OpenConnection();

            int lecturerId = CMCSDB.FindLecturer(CMCSMain.User.IdentityNumber);
            string sql = $"SELECT * FROM Request r INNER JOIN RequestProcess p ON r.RequestID = p.RequestID WHERE LecturerID = {lecturerId}";
            SqlDataReader reader = CMCSDB.RunSQLResult(sql);

            sb.AppendLine("Accepted/Rejected Request(s):");

            while(reader.Read())
            {
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

            reader.Close();

            string sql2 = $"SELECT * FROM Request WHERE LecturerID = {lecturerId} AND RequestStatus = 'Pending'";
            reader = CMCSDB.RunSQLResult(sql2);

            sb.AppendLine("Pending Request(s):");

            while(reader.Read())
            {
                string requestID = reader["RequestID"].ToString();
                string requestFor = reader["RequestFor"].ToString();
                string hoursWorked = reader["HoursWorked"].ToString();
                string hourlyRate = reader["HourlyRate"].ToString();
                string description = reader["Description"].ToString();
                string requestStatus = reader["RequestStatus"].ToString();
                string dateSubmitted = DateTime.Parse(reader["DateSubmitted"].ToString()).ToString("dd/MM/yyyy HH:mm:ss");

                sb.AppendLine($"Request ID: {requestID}; Request For: {requestFor}; Hours Worked: {hoursWorked}; Hourly Rate: {hourlyRate}; Description: {description}; Date Submitted: {dateSubmitted}");
            }

            reader.Close();
            CMCSDB.CloseConnection();

            this.Response.Headers.Add("FileName", Guid.NewGuid().ToString().ToUpper());
            this.Response.WriteAsync(sb.ToString());
            this.Response.CompleteAsync();

            return View();
        }
    }
}
