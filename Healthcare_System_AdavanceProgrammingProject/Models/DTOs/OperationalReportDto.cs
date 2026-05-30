

namespace HealthcareClinic.API.Models.DTOs
{
    public class OperationalReportDto
    {
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int MissedAppointments { get; set; }
        public double MissedRate { get; set; }
        public List<DoctorUtilizationDto> DoctorUtilizationMetrics { get; set; } = new();
    }

    public class DoctorUtilizationDto
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int BookedAppointmentsCount { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}