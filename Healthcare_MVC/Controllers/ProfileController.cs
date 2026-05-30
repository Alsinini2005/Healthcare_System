using BCrypt.Net;
using HealthcareClinic.API.Data;
using Healthcare_System_AdavanceProgrammingProject.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ClinicDbContext _context;

        public ProfileController(ClinicDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var email = User.Identity?.Name;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound();

            var vm = new ProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var email = User.Identity?.Name;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound();

            // Validate current password
            bool validPassword = BCrypt.Net.BCrypt.Verify(
                model.CurrentPassword,
                user.PasswordHash);

            if (!validPassword)
            {
                TempData["ErrorMessage"] =
                    "Current password is incorrect.";

                return View(model);
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                TempData["ErrorMessage"] =
                    "New password cannot be empty.";

                return View(model);
            }

            if (model.NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] =
                    "Password must be at least 6 characters.";

                return View(model);
            }

            // Confirm password check
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] =
                    "Passwords do not match.";

                return View(model);
            }

            // Hash new password
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    model.NewPassword);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Password updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
