using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class AccountRecovery
    {
        [Key]
        [Required]
        public int AccountRecoveryID { get; set; }
        [Required]
        public string? Method { get; set; }
        [Required]
        public string? Value { get; set; }
        [Required]
        public string? UserID { get; set; }
    }
}
