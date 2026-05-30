using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    [Authorize]
    public class DoctorsController : Controller
    {
        private readonly ClinicDbContext _context;

        public DoctorsController(ClinicDbContext context)
        {
            _context = context;
        }

        // ========================= INDEX =========================

        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .Include(d => d.User)
                .OrderByDescending(d => d.User != null && d.User.IsActive)
                .ToListAsync();

            ViewBag.AllSpecializations =
                await _context.Specializations.ToListAsync();

            return View(doctors);
        }

        // ========================= CREATE =========================
        // FIX #3: Add Doctor flow — creates a User (Role=Doctor) AND a Doctor row atomically.
        // This guarantees the new doctor appears in both tables.

        [Authorize(Roles = "ClinicManager")]
        public async Task<IActionResult> Create()
        {
            // FIX #2: pass specializations list so the form can assign specializations on creation
            ViewBag.AllSpecializations = await _context.Specializations.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ClinicManager")]
        public async Task<IActionResult> Create(Doctor doctor, string password, List<int> specializationIds)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("User");
            ModelState.Remove("DoctorSpecializations");
            ModelState.Remove("Appointments");
            ModelState.Remove("VisitRecords");

            bool cprExists = await _context.Doctors
            .AnyAsync(d => d.CPR == doctor.CPR);

            if (cprExists)
            {
                ModelState.AddModelError("CPR", "CPR already exists.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.AllSpecializations = await _context.Specializations.ToListAsync();
                return View(doctor);
            }

            // FIX #1: Create the User account first so the doctor appears in Users table
            var hasher = new HealthcareClinic.API.Services.PasswordHasher();
            var user = new User
            {
                Name = doctor.Name,
                Email = doctor.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                 string.IsNullOrWhiteSpace(password)
                ? "password123"
                : password),
                Role = "Doctor",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // generate user.Id

            // Now create the Doctor row linked to the new User
            doctor.UserId = user.Id;
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync(); // generate doctor.Id

            // FIX #2: Save many-to-many specialization links
            foreach (var specId in specializationIds)
            {
                _context.DoctorSpecializations.Add(new DoctorSpecialization
                {
                    DoctorId = doctor.Id,
                    SpecializationId = specId
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Doctor created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ========================= EDIT =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ClinicManager")]
        public async Task<IActionResult> Edit(
    int id,
    Doctor doctor,
    List<int> specializationIds)
        {
            if (id != doctor.Id)
                return NotFound();

            ModelState.Remove("User");
            ModelState.Remove("DoctorSpecializations");
            ModelState.Remove("Appointments");
            ModelState.Remove("VisitRecords");

            // CPR duplicate validation
            bool cprExists = await _context.Doctors
                .AnyAsync(d => d.CPR == doctor.CPR && d.Id != doctor.Id);

            if (cprExists)
            {
                ModelState.AddModelError("CPR", "CPR already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AllSpecializations =
                    await _context.Specializations.ToListAsync();

                return View(doctor);
            }

            var existingDoctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorSpecializations)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (existingDoctor == null)
                return NotFound();

            // Update Doctor table
            existingDoctor.Name = doctor.Name;
            existingDoctor.Email = doctor.Email;
            existingDoctor.CPR = doctor.CPR;

            existingDoctor.WorkingHours = doctor.WorkingHours;
            existingDoctor.DaysOff = doctor.DaysOff;

            // Sync linked User table
            if (existingDoctor.User != null)
            {
                existingDoctor.User.Name = doctor.Name;
                existingDoctor.User.Email = doctor.Email;
                existingDoctor.User.CPR = doctor.CPR;
            }

            // Update specializations
            _context.DoctorSpecializations.RemoveRange(
                existingDoctor.DoctorSpecializations);

            foreach (var specId in specializationIds)
            {
                _context.DoctorSpecializations.Add(
                    new DoctorSpecialization
                    {
                        DoctorId = id,
                        SpecializationId = specId
                    });
            }

            var futureAppointments = await _context.Appointments
                .Where(a =>
                    a.DoctorId == id &&
                    a.AppointmentDate > DateTime.Now &&
                    a.Status != "Cancelled")
                .ToListAsync();

            foreach (var appointment in futureAppointments)
            {
                appointment.Status = "Reschedule Required";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Doctor updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ========================= SET STATUS (FIX #4) =========================
        // Replaces "Add Schedule" button with a toggle to activate/deactivate a doctor.

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ClinicManager")]
        public async Task<IActionResult> SetStatus(int id, bool isActive)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            if (doctor.User != null)
                doctor.User.IsActive = isActive;

            // If deactivating: cancel future appointments
            if (!isActive)
            {
                var futureAppointments = await _context.Appointments
                    .Where(a =>
                        a.DoctorId == id &&
                        a.AppointmentDate > DateTime.Now &&
                        a.Status != "Cancelled" &&
                        a.Status != "Completed" &&
                        a.Status != "Missed")
                    .ToListAsync();

                foreach (var appt in futureAppointments)
                    appt.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = isActive
                ? $"Dr. {doctor.Name} is now Active."
                : $"Dr. {doctor.Name} has been set to Inactive. Future appointments cancelled.";

            return RedirectToAction(nameof(Index));
        }

        // ========================= DELETE =========================

        [Authorize(Roles = "ClinicManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ClinicManager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            // Remove specialization links first
            var doctorSpecs = await _context.DoctorSpecializations
                .Where(ds => ds.DoctorId == id)
                .ToListAsync();

            _context.DoctorSpecializations.RemoveRange(doctorSpecs);

            // Remove doctor row
            _context.Doctors.Remove(doctor);

            // Remove linked user row
            if (doctor.User != null)
            {
                _context.Users.Remove(doctor.User);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Doctor deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
