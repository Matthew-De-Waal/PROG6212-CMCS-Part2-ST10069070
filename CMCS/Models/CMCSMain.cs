using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Data.SqlClient;

namespace CMCS.Models
{
    /// <summary>
    /// Static Class: CMCSMain
    /// </summary>
    public static class CMCSMain
    {
        // Automatic Properties
        public static User User { get; set; } = new User();
        public static int SelectedRequestIndex { get; set; } = -1;
        public static int SelectedRequestID { get; set; }

        /// <summary>
        /// This method gets the document type from a given filename.
        /// </summary>
        /// <param name="documentName"></param>
        /// <returns></returns>
        public static string GetDocumentType(string documentName)
        {
            string type = string.Empty;

            if (documentName.ToLower().EndsWith(".pdf"))
                type = "PDF";
            else if (documentName.ToLower().EndsWith(".xlsx"))
                type = "XLSX";
            else if (documentName.ToLower().EndsWith(".png"))
                type = "PNG";
            else if (documentName.ToLower().EndsWith(".jpg"))
                type = "JPG";
            else
                type = "Unknown";

            return type;
        }

        /// <summary>
        /// This method checks if the user already exists in the database.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<bool> UserExists(string userId)
        {
            bool result = false;

            var reader1 = await CMCSDB.RunSQLResult($"SELECT * FROM Lecturer WHERE IdentityNumber = '{userId}'");
            bool reader1Result = reader1 != null ? reader1.HasRows : false;
            await CMCSDB.CloseReader();

            var reader2 = await CMCSDB.RunSQLResult($"SELECT * FROM Manager WHERE IdentityNumber = '{userId}'");
            bool reader2Result = reader2 != null ? reader2.HasRows : false;
            await CMCSDB.CloseReader();

            result = reader1Result || reader2Result;

            return result;
        }

        /// <summary>
        /// Generates a random key.
        /// </summary>
        /// <param name="length">The length of the key.</param>
        /// <returns></returns>
        public static string GenerateKey(int length)
        {
            const string SYMBOLS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string output = string.Empty;

            for(int i = 0; i < length; i++)
            {
                output += SYMBOLS[Random.Shared.Next(0, SYMBOLS.Length - 1)];
            }

            return output;
        }
    }
}
