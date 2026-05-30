namespace HealthcareClinic.API.Models.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "Requested"; // Requested → Confirmed → CheckedIn → InProgress → Completed / Cancelled / Missed
        public string Specialty { get; set; } = string.Empty;
        public string DoctorNotes { get; set; } = string.Empty;

        // Navigation properties
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }
        public VisitRecord? VisitRecord { get; set; }
    }
}
