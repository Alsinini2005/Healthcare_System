
namespace HealthcareClinic.API.Models.Entities
{
    public class Specialization
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Navigation property - many-to-many via join table
        public List<DoctorSpecialization> DoctorSpecializations { get; set; } = new();
    }
}
