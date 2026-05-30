
namespace HealthcareClinic.API.Models.Entities
{
    public class Prescription
    {
        public int Id { get; set; }
        public int VisitRecordId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        
        public string Frequency { get; set; } = string.Empty;  // e.g., "Twice daily"
        public string Dosage { get; set; } = string.Empty;     // e.g., "500mg"
        public string Duration { get; set; } = string.Empty;   // e.g., "7 days"

        // Navigation property
        public VisitRecord? VisitRecord { get; set; }
    }
}
