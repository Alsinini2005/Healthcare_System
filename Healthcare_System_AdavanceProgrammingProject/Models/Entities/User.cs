using System.ComponentModel.DataAnnotations;

namespace HealthcareClinic.API.Models.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        [Required(ErrorMessage = "CPR is required.")]
        [Range(100000000, 999999999,
    ErrorMessage = "CPR must contain exactly 9 digits.")]
        public long CPR { get; set; }
        public string Role { get; set; } = string.Empty; // Patient, Doctor, Receptionist, ClinicManager
        public bool IsActive { get; set; } = true; // FIX #4: doctor active/inactive status

        // Navigation property
        public List<Notification> Notifications { get; set; } = new();
        public Doctor? DoctorProfile { get; set; }
    }
}
