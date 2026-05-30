

namespace HealthcareClinic.API.Models.Entities
{
    // Join table for the many-to-many relationship between Doctor and Specialization
    public class DoctorSpecialization
    {
        public int DoctorId { get; set; }

        public int SpecializationId { get; set; }

        // Navigation properties
        public Doctor? Doctor { get; set; }

        public Specialization? Specialization { get; set; }
    }
}
