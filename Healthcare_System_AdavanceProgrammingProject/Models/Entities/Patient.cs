
namespace HealthcareClinic.API.Models.Entities
{
    public class Patient
    {
        public int Id { get; set; }

        // Link to the User account
        public int UserId { get; set; }
        public string PatientReferenceNumber { get; set; } = string.Empty; // Unique token for public tracking without login

        public string CPRNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        

        // Navigation properties
        public User? User { get; set; }
        public List<Appointment> Appointments { get; set; } = new();
        public List<VisitRecord> VisitRecords { get; set; } = new();
    }
}
