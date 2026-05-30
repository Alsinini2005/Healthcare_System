

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareClinic.API.Models.Entities
{
    public class DoctorSchedule
    {
        [Key]
        public int Id { get; set; }


        [Required]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }


        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }


        public bool IsAvailable { get; set; } = true;
    }
}