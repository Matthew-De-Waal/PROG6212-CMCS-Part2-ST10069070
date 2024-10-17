namespace CMCS.Models
{
    public class User
    {
        // Automatic Properties
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? IdentityNumber { get; set; }
        public string? EmailAddress { get; set; }
        public bool LoggedOn { get; set; }
        public bool IsManager { get; set;  }
    }
}
