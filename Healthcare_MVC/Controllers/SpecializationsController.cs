using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{

    [Authorize(Roles = "ClinicManager")]
    public class SpecializationsController : Controller
    {
        private readonly ClinicDbContext _context;

        public SpecializationsController(ClinicDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.Specializations.ToListAsync();
            return View(list);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var specialization = await _context.Specializations
                .FirstOrDefaultAsync(s => s.Id == id);

            if (specialization == null)
            {
                TempData["ErrorMessage"] = "Specialty not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Specializations.Remove(specialization);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Specialty removed successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Specialization model)
        {
            if (ModelState.IsValid)
            {
                _context.Specializations.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Specialization added successfully.";

                return RedirectToAction(nameof(Index));
            }


            return View(model);
        }
    }
}
