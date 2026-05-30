using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using HealthcareClinic.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    [Authorize(Roles = "Receptionist,ClinicManager")]
    public class PatientsController : Controller
    {
        private readonly ClinicDbContext _context;

        public PatientsController(ClinicDbContext context)
        {
            _context = context;
        }

        // GET: /Patients
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients.ToListAsync();
            return View(patients);
        }

        // GET: /Patients/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Patients/Create
        // Creates a User account and links it to the Patient profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string cprNumber, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(cprNumber)
                || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "All fields are required to register a new patient.";
                return RedirectToAction(nameof(Index));
            }

            // Check if email already taken
            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
            if (emailExists)
            {
                TempData["ErrorMessage"] = "A user account with this email already exists.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Create the User account first
            var user = new User
            {
                Name = name,
                Email = email,
                Role = "Patient",
                PasswordHash = PasswordHasher.Hash(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Save to get the generated UserId

            // 2. Generate reference number and create the Patient profile linked to the User
            var refToken = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
            var patient = new Patient
            {
                UserId = user.Id,
                Name = name,
                CPRNumber = cprNumber,
                PatientReferenceNumber = $"REF-2026-{refToken}"
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Patient registered! Reference code: {patient.PatientReferenceNumber}";
            return RedirectToAction(nameof(Index));
        }
    }
}