using System.ComponentModel.DataAnnotations;

namespace HealthcareClinic.API.Models.Entities
{
    public class Doctor
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "CPR is required.")]
        [Range(100000000, 999999999,
    ErrorMessage = "CPR must contain exactly 9 digits.")]
        public long CPR { get; set; }

        public string WorkingHours { get; set; } = "08:00 - 16:00";

        public string DaysOff { get; set; } = "Friday, Saturday";

        public User? User { get; set; }

        public List<DoctorSpecialization> DoctorSpecializations { get; set; } = new();

        public List<Appointment> Appointments { get; set; } = new();

        public List<VisitRecord> VisitRecords { get; set; } = new();
    }
}