using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class AccountRecovery
    {
        [Key]
        public int AccountRecoveryID { get; set; }
        public string? Method { get; set; }
        public string? Value { get; set; }
        public string? UserID { get; set; }
    }
}
