

namespace HealthcareClinic.ReportingApp.Models
{
    // Result from api/auth/login
    public class LoginResultModel
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    // Result from api/reports/dashboard-stats
    public class DashboardStatsModel
    {
        public int CompletedAppointments { get; set; }
        public int TotalAppointments { get; set; }
        
        public int MissedAppointments { get; set; }
    }

    // Result from api/reports/operational
    public class OperationalReportModel
    {
        public int TotalAppointments { get; set; }
        
        public int MissedAppointments { get; set; }

        public double MissedRate { get; set; }
        public int CompletedAppointments { get; set; }
        public List<DoctorUtilizationModel> DoctorUtilizationMetrics { get; set; } = new();

    }

    public class DoctorUtilizationModel
    {

        public int DoctorId { get; set; }
        public int BookedAppointmentsCount { get; set; }
        public string DoctorName { get; set; } = string.Empty;

       
        public double UtilizationPercentage { get; set; }
    }

    // Login form view model
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}