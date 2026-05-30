using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HealthcareClinic.API.Data;
using HealthcareClinic.API.Hubs;

namespace HealthcareClinic.API.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly IHubContext<AppointmentHub> _hubContext;

        // Full appointment lifecycle as required by the brief
        private static readonly string[] ValidStatuses =
        {
            "Requested",
            "InProgress",
            "Completed",
            "Missed"
        };

        // Valid transitions — enforces correct lifecycle order
        
        private static readonly Dictionary<string, string[]> AllowedTransitions = new()
        {
            { "Requested",  new[] { "InProgress", "Missed" } },

            { "InProgress", new[] { "Completed", "Missed" } },

            { "Completed", Array.Empty<string>() },

            { "Missed", Array.Empty<string>() }
        };

        public AppointmentsApiController(ClinicDbContext context, IHubContext<AppointmentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private async Task UpdateMissedAppointments()
        {
            var missedAppointments = await _context.Appointments
                .Where(a =>
                    (a.Status == "Requested" ||
                     a.Status == "InProgress")
                    &&
                    a.AppointmentDate < DateTime.Now.AddHours(-1))
                .ToListAsync();

            foreach (var appointment in missedAppointments)
            {
                appointment.Status = "Missed";
            }

            await _context.SaveChangesAsync();
        }

        // PUT: api/appointments/update-status/1?status=Confirmed

        [HttpPut("update-status/{id}")]
        [Authorize(Roles = "Receptionist,Doctor,ClinicManager")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            await UpdateMissedAppointments();
            if (!ValidStatuses.Contains(status))
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", ValidStatuses)}" });

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound(new { message = "Appointment not found." });

            // Enforce valid lifecycle transitions
            if (!AllowedTransitions[appointment.Status].Contains(status))
                return BadRequest(new { message = $"Cannot transition from '{appointment.Status}' to '{status}'." });

            appointment.Status = status;
            await _context.SaveChangesAsync();

            // Broadcast real-time status update via SignalR
            string patientName = appointment.Patient?.Name ?? "Patient";
            await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", appointment.Id, patientName, status);

            return Ok(new { message = $"Status updated to '{status}' and broadcasted live." });
        }
    }
}
