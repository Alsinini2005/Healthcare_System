
namespace HealthcareClinic.API.Models.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }

        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "Requested"; // Requested →  CheckedIn → InProgress → Completed  / Missed

        public string DoctorNotes { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;

        // Navigation properties

        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }
        public VisitRecord? VisitRecord { get; set; }
    }
}
