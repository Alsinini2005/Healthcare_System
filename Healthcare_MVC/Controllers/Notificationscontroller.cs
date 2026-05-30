using HealthcareClinic.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ClinicDbContext _context;

        public NotificationsController(ClinicDbContext context)
        {
            _context = context;
        }

        // GET: /Notifications/MarkAllRead
        public async Task<IActionResult> MarkAllRead()
        {
            var email = User.Identity?.Name ?? string.Empty;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user != null)
            {
                var unread = await _context.Notifications
                    .Where(n => n.UserId == user.Id && !n.IsRead)
                    .ToListAsync();

                foreach (var n in unread)
                    n.IsRead = true;

                await _context.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}