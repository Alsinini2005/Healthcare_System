using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using HealthcareClinic.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    [Authorize(Roles = "ClinicManager")]
    public class ReceptionistsController : Controller
    {
        private readonly ClinicDbContext _context;

        public ReceptionistsController(ClinicDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var receptionists = await _context.Users
                .Where(u => u.Role == "Receptionist")
                .ToListAsync();

            return View(receptionists);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string email,
            string password,
            String cpr)
        {
            if (string.IsNullOrWhiteSpace(cpr) || !System.Text.RegularExpressions.Regex.IsMatch(cpr, @"^\d{9}$"))
            {
                TempData["ErrorMessage"] = "CPR must be exactly 9 digits (numbers only).";
                return View();
            }
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                TempData["ErrorMessage"] =
                    "Email already exists.";

                return View();
            }

            bool cprExists = await _context.Users
                .AnyAsync(u => u.CPR == cpr);

            if (cprExists)
            {
                TempData["ErrorMessage"] =
                    "CPR already exists.";

                return View();
            }

            var receptionist = new User
            {
                Name = name,
                Email = email,
                CPR = cpr,
                Role = "Receptionist",
                PasswordHash = PasswordHasher.Hash(password)
            };

            _context.Users.Add(receptionist);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receptionist added successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var receptionist = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.Role == "Receptionist");

            if (receptionist == null)
            {
                TempData["ErrorMessage"] =
                    "Receptionist not found.";

                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(receptionist);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receptionist removed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
