namespace HealthcareClinic.API.Models.Entities
{
    public class VisitRecord
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }

        // No separate PatientId/DoctorId — they are already on the Appointment
        public DateTime VisitDate { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string DoctorNotes { get; set; } = string.Empty;
        public string PrescribedTreatment { get; set; } = string.Empty;

        // Navigation properties
        public Appointment? Appointment { get; set; }
        public List<Prescription> Prescriptions { get; set; } = new();
    }
}
