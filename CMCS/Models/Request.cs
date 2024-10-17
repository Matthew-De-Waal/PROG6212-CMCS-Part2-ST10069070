namespace CMCS.Models
{
    public class Request
    {
        // Automatic Properties
        public int RequestID { get; set; }
        public int LecturerID { get; set; }
        public string? RequestFor { get; set; }
        public int HoursWorked { get; set; }
        public int HourlyRate { get; set; }
        public string? Description { get; set; }
        public string? RequestStatus { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public DateTime? DateApproved { get; set; }
    }
}
