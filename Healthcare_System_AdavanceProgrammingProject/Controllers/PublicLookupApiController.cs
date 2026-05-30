using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.DTOs;

namespace HealthcareClinic.API.Controllers
{
    [ApiController]
    [Route("api/public-lookup")]
    [AllowAnonymous]
    public class PublicLookupApiController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public PublicLookupApiController(ClinicDbContext context)
        {
            _context = context;
        }

        // GET: api/public-lookup/find?cpr=990123456&refNum=REF-2026-XYZ
        [HttpGet("find")]
        public async Task<IActionResult> GetUpcomingAppointments([FromQuery] string cpr, [FromQuery] string refNum)
        {
            // Load patient with appointments and their doctors in one query
            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(p => p.CPRNumber == cpr && p.PatientReferenceNumber == refNum);

            if (patient == null)
                return NotFound(new { message = "No matching patient records found." });

            // Load most recent visit record separately (PatientId was removed from VisitRecord — access via Appointment)
            var recentDiagnosis = await _context.VisitRecords
                .Where(v => v.Appointment.PatientId == patient.Id)
                .OrderByDescending(v => v.VisitDate)
                .Select(v => v.Diagnosis)
                .FirstOrDefaultAsync() ?? "No past clinical diagnostic history found.";

            var upcomingData = patient.Appointments
                .Where(a => a.Status != "Completed" && a.Status != "Cancelled" && a.Status != "Missed")
                .Select(a => new PublicAppointmentDto
                {
                    AppointmentId = a.Id,
                    PatientReferenceNumber = patient.PatientReferenceNumber,
                    PatientName = patient.Name,
                    DoctorName = a.Doctor?.Name ?? "Assigned Doctor",
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Specialty = a.Specialty,
                    RecentVisitSummary = recentDiagnosis
                }).ToList();

            return Ok(upcomingData);
        }
        
    }
}
