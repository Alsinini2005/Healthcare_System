namespace HealthcareClinic.API.Models.DTOs
{
    public class PublicAppointmentDto
    {
        public int AppointmentId { get; set; }
        public string PatientReferenceNumber { get; set; } = string.Empty; // Used as the public lookup key
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string RecentVisitSummary { get; set; } = "No past medical encounters archived.";
    }

    // Used as the request body when a user submits the public tracking form
    public class PublicTrackingRequestDto
    {
        public string PatientReferenceNumber { get; set; } = string.Empty;
    }
}