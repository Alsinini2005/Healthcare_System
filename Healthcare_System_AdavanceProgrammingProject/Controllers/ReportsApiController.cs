using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.DTOs;

namespace HealthcareClinic.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "ClinicManager")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public ReportsApiController(ClinicDbContext context)
        {
            _context = context;
        }

        // GET: api/reports/dashboard-stats
        // Used by the Reporting Application to display quick stats
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var total = await _context.Appointments.CountAsync();
            var completed = await _context.Appointments.CountAsync(a => a.Status == "Completed");
            var missed = await _context.Appointments.CountAsync(a => a.Status == "Missed");

            return Ok(new
            {
                totalAppointments = total,
                completedAppointments = completed,
                missedAppointments = missed
            });
        }

        // GET: api/reports/operational
        // Full operational report with doctor utilization — used by the Reporting Application
        [HttpGet("operational")]
        public async Task<IActionResult> GetOperationalReport()
        {
            // Load all appointments once into memory
            var appointments = await _context.Appointments.ToListAsync();
            int total = appointments.Count;

            if (total == 0) return Ok(new OperationalReportDto());

            int completed = appointments.Count(a => a.Status == "Completed");
            int missed = appointments.Count(a => a.Status == "Missed");

            // Group by DoctorId in memory — avoids subquery per doctor
            var appointmentsByDoctor = appointments
                .GroupBy(a => a.DoctorId)
                .ToDictionary(g => g.Key, g => g.Count());

            var doctors = await _context.Doctors.ToListAsync();

            var utilizationList = doctors.Select(d => new DoctorUtilizationDto
            {
                DoctorId = d.Id,
                DoctorName = d.Name,
                BookedAppointmentsCount = appointmentsByDoctor.GetValueOrDefault(d.Id, 0),
                UtilizationPercentage = Math.Round(
                    ((double)appointmentsByDoctor.GetValueOrDefault(d.Id, 0) / total) * 100, 2)
            }).ToList();

            var report = new OperationalReportDto
            {
                TotalAppointments = total,
                CompletedAppointments = completed,
                MissedAppointments = missed,
                MissedRate = Math.Round(((double)missed / total) * 100, 2),
                DoctorUtilizationMetrics = utilizationList
            };

            return Ok(report);
        }
        
    }
}
