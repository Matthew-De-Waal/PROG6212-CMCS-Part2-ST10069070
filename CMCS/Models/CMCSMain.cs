using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Data.SqlClient;

namespace CMCS.Models
{
    public static class CMCSMain
    {
        public static CMCSUser User { get; set; } = new CMCSUser();
        public static int SelectedRequestIndex { get; set; } = -1;
        public static int SelectedRequestID { get; set; }

        public static string GetDocumentType(string documentName)
        {
            string type = string.Empty;

            if (documentName.ToLower().EndsWith(".pdf"))
                type = "PDF";

            if (documentName.ToLower().EndsWith(".xlsx"))
                type = "XLSX";

            if (documentName.ToLower().EndsWith(".png"))
                type = "PNG";

            if (documentName.ToLower().EndsWith(".jpg"))
                type = "JPG";

            return type;
        }

        public static bool UserExists(string userId)
        {
            bool result = false;

            var reader1 = CMCSDB.RunSQLResult($"SELECT * FROM Lecturer WHERE IdentityNumber = '{userId}'");
            bool reader1Result = reader1.HasRows;
            reader1.Close();

            var reader2 = CMCSDB.RunSQLResult($"SELECT * FROM Manager WHERE IdentityNumber = '{userId}'");
            bool reader2Result = reader2.HasRows;
            reader2.Close();

            result = reader1Result || reader2Result;

            return result;
        }
    }
}
