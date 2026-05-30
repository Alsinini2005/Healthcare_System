using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using HealthcareClinic.API.Services;
using Healthcare_System_AdavanceProgrammingProject.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ClinicDbContext _context;
        private readonly NotificationService _notificationService;

        public DashboardController(ClinicDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {

            ViewBag.JwtToken = HttpContext.Session.GetString("JwtToken");
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Patient";
            var email = User.Identity?.Name ?? string.Empty;

            var viewModel = new DashboardViewModel
            {
                CurrentUserRole = role,
                CurrentUserName = email.Split('@')[0]
            };

            if (role == "Receptionist")
            {
                viewModel.ActiveAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a =>
                    a.Status == "Requested" ||
                    a.Status == "InProgress")
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

                viewModel.ClinicDoctors = await _context.Doctors
                    .Include(d => d.DoctorSpecializations)
                        .ThenInclude(ds => ds.Specialization)
                    .Include(d => d.User)
                    .ToListAsync();

                viewModel.ClinicPatients = await _context.Patients.ToListAsync();
                ViewBag.AllSpecializations =
    await _context.Specializations.ToListAsync();

                return View("Receptionist", viewModel);
            }
            else if (role == "ClinicManager")
            {
                var doctors = await _context.Doctors
                    .Include(d => d.DoctorSpecializations)
                        .ThenInclude(ds => ds.Specialization)
                    .Include(d => d.User)
                    .ToListAsync();

                viewModel.ActiveAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(10)
                    .ToListAsync();

                viewModel.ClinicDoctors = doctors;
                viewModel.ClinicPatients = await _context.Patients.ToListAsync();

                ViewBag.TotalDoctors = doctors.Count;
                ViewBag.TotalPatients = viewModel.ClinicPatients.Count;
                ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
                ViewBag.TotalSpecializations = await _context.Specializations.CountAsync();

                return View("ClinicManagerDashboard", viewModel);
            }
            else if (role == "Doctor")
            {
                var doctorProfile = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.Email.ToLower() == email.ToLower());
                int docId = doctorProfile?.Id ?? 0;

                viewModel.ActiveAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == docId &&
                                a.Status != "Completed" &&
                                a.Status != "Cancelled" &&
                                a.Status != "Missed")
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();

                viewModel.PastMedicalHistory = await _context.VisitRecords
                    .Include(v => v.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Include(v => v.Prescriptions)
                    .Where(v => v.Appointment.DoctorId == docId)
                    .OrderByDescending(v => v.VisitDate)
                    .ToListAsync();

                return View("Doctor", viewModel);
            }
            else
            {
                // Patient
                var patientProfile = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email.ToLower() == email.ToLower());

                viewModel.ActiveAppointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.VisitRecord)
                        .ThenInclude(v => v!.Prescriptions)
                    .Where(a => a.PatientId == patientProfile!.Id)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();

                // FIX #6: load specializations and doctors with their specializations
                viewModel.ClinicDoctors = await _context.Doctors
                    .Include(d => d.DoctorSpecializations)
                        .ThenInclude(ds => ds.Specialization)
                    .Include(d => d.User)
                    .ToListAsync();

                ViewBag.PatientId = patientProfile!.Id;
                // FIX #6: pass specializations list separately for the "select specialty first" dropdown
                ViewBag.AllSpecializations = await _context.Specializations.ToListAsync();
                var expiredAppointments = await _context.Appointments
    .Include(a => a.VisitRecord)
    .Where(a =>
        a.Status == "InProgress" &&
        a.AppointmentDate.AddHours(1) <= DateTime.Now)
    .ToListAsync();

                foreach (var appointment in expiredAppointments)
                {
                    // Skip if already completed automatically
                    if (appointment.VisitRecord != null)
                        continue;

                    appointment.Status = "Completed";

                    var autoVisit = new VisitRecord
                    {
                        AppointmentId = appointment.Id,
                        VisitDate = DateTime.Now,
                        Diagnosis = "Doctor did not submit diagnosis.",
                        DoctorNotes = "Visit was automatically completed by the system after exceeding 1 hour.",
                        PrescribedTreatment = "No treatment was submitted."
                    };

                    _context.VisitRecords.Add(autoVisit);

                    var autoPrescription = new Prescription
                    {
                        VisitRecord = autoVisit,
                        MedicationName = "No medication submitted",
                        Dosage = "N/A",
                        Frequency = "N/A",
                        Duration = "N/A"
                    };

                    _context.Prescriptions.Add(autoPrescription);
                }

                await _context.SaveChangesAsync();
                return View("Patient", viewModel);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound();

            appointment.Status = status;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ── Patient self-booking ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment(int doctorId,
    DateTime appointmentDate, string specialty)
        {
            // Resolve logged-in user safely from claims
            var loggedInEmail = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(loggedInEmail))
            {
                TempData["ErrorMessage"] = "Your session expired. Please login again.";
                return RedirectToAction("Login", "Account");
            }

            var patientProfile = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User != null &&
                                          p.User.Email == loggedInEmail);

            if (patientProfile == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found.";
                return RedirectToAction(nameof(Index));
            }

            if (appointmentDate.Year < DateTime.Now.Year || appointmentDate < DateTime.Now)
            {
                TempData["ErrorMessage"] =
                    "Appointment date must be in the future.";
                return RedirectToAction(nameof(Index));
            }

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent booking inactive doctors
            if (doctor.User == null || !doctor.User.IsActive)
            {
                TempData["ErrorMessage"] =
                    $"Dr. {doctor.Name} is currently inactive.";
                return RedirectToAction(nameof(Index));
            }

            string dayName = appointmentDate.DayOfWeek.ToString();

            if (!string.IsNullOrWhiteSpace(doctor.DaysOff) &&
                doctor.DaysOff.Contains(dayName))
            {
                TempData["ErrorMessage"] =
                    $"Dr. {doctor.Name} is off on {dayName}s.";
                return RedirectToAction(nameof(Index));
            }

            var parts = doctor.WorkingHours.Split('-');

            if (parts.Length == 2 &&
                TimeSpan.TryParse(parts[0].Trim(), out var start) &&
                TimeSpan.TryParse(parts[1].Trim(), out var end))
            {
                if (appointmentDate.TimeOfDay < start ||
                    appointmentDate.TimeOfDay > end)
                {
                    TempData["ErrorMessage"] =
                        $"Dr. {doctor.Name}'s working hours are {doctor.WorkingHours}.";
                    return RedirectToAction(nameof(Index));
                }
            }

            bool alreadyBooked = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate == appointmentDate &&
                a.Status != "Cancelled" &&
                a.Status != "Missed");

            if (alreadyBooked)
            {
                TempData["ErrorMessage"] =
                    $"Dr. {doctor.Name} already has an appointment at that time.";
                return RedirectToAction(nameof(Index));
            }

            var appointment = new Appointment
            {
                PatientId = patientProfile.Id,
                DoctorId = doctorId,
                AppointmentDate = appointmentDate,
                Status = "Requested",
                Specialty = specialty,
                DoctorNotes = string.Empty
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await _notificationService.NotifyAppointmentBookedAsync(appointment);

            TempData["SuccessMessage"] =
                "Your appointment has been requested successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ── Receptionist / ClinicManager booking ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Receptionist")]
        public async Task<IActionResult> StartVisit(int appointmentId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.Status != "Requested")
            {
                TempData["ErrorMessage"] =
                    "Only requested appointments can be started.";

                return RedirectToAction(nameof(Index));
            }

            appointment.Status = "InProgress";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Appointment moved to In Progress.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Receptionist,ClinicManager")]
        public async Task<IActionResult> CreateAppointment(DashboardViewModel input,string PatientCPR)
        {
            var patient = await _context.Patients
    .FirstOrDefaultAsync(p => p.CPRNumber == PatientCPR);

            if (patient == null)
            {
                TempData["ErrorMessage"] =
                    "Please register an account first.";

                return RedirectToAction(nameof(Index));
            }

            input.NewAppointmentPlaceholder.PatientId = patient.Id;
            var requestedDate = input.NewAppointmentPlaceholder.AppointmentDate == DateTime.MinValue
                ? DateTime.Now.AddDays(2)
                : input.NewAppointmentPlaceholder.AppointmentDate;

            // FIX #5: Guard against wrong-year bookings
            if (requestedDate.Year < DateTime.Now.Year || requestedDate < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Appointment date must be in the future. Please choose a valid date and time.";
                return RedirectToAction(nameof(Index));
            }

            var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d =>
            d.Id == input.NewAppointmentPlaceholder.DoctorId);

            if (doctor?.User == null || !doctor.User.IsActive)
            {
                TempData["ErrorMessage"] =
                    "Cannot book with an inactive doctor.";

                return RedirectToAction(nameof(Index));
            }

            if (doctor != null)
            {
                string dayName = requestedDate.DayOfWeek.ToString();
                if (doctor.DaysOff.Contains(dayName))
                {
                    TempData["ErrorMessage"] = $"Dr. {doctor.Name} is off on {dayName}s.";
                    return RedirectToAction(nameof(Index));
                }

                var parts = doctor.WorkingHours.Split('-');
                if (parts.Length == 2 &&
                    TimeSpan.TryParse(parts[0].Trim(), out var start) &&
                    TimeSpan.TryParse(parts[1].Trim(), out var end))
                {
                    if (requestedDate.TimeOfDay < start || requestedDate.TimeOfDay > end)
                    {
                        TempData["ErrorMessage"] = $"Dr. {doctor.Name}'s working hours are {doctor.WorkingHours}.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                bool alreadyBooked = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == input.NewAppointmentPlaceholder.DoctorId &&
                    a.AppointmentDate == requestedDate &&
                    a.Status != "Cancelled" &&
                    a.Status != "Missed");

                if (alreadyBooked)
                {
                    TempData["ErrorMessage"] = $"Dr. {doctor.Name} already has an appointment at that time.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var appointment = new Appointment
            {
                PatientId = input.NewAppointmentPlaceholder.PatientId,
                DoctorId = input.NewAppointmentPlaceholder.DoctorId,
                AppointmentDate = requestedDate,
                Status = "Requested",
                Specialty = input.NewAppointmentPlaceholder.Specialty ?? "General Practice",
                DoctorNotes = string.Empty
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await _notificationService.NotifyAppointmentBookedAsync(appointment);

            TempData["SuccessMessage"] = "Appointment booked successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── Doctor logs visit ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> LogVisitAndDiagnosis(
        DashboardViewModel input,string medicationName,string dosage,string frequency,string duration)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.Id == input.NewVisitPlaceholder.AppointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            // Only allow completing appointments already in progress
            if (appointment.Status != "InProgress")
            {
                TempData["ErrorMessage"] =
                    "Only appointments marked as InProgress can be completed.";

                return RedirectToAction(nameof(Index));
            }

            var visit = new VisitRecord
            {
                AppointmentId = input.NewVisitPlaceholder.AppointmentId,
                VisitDate = DateTime.Now,
                Diagnosis = input.NewVisitPlaceholder.Diagnosis,

                DoctorNotes =
                    input.NewVisitPlaceholder.DoctorNotes
                    ?? string.Empty,

                PrescribedTreatment =
                    input.NewVisitPlaceholder.PrescribedTreatment
                    ?? "Rest and monitoring."
            };

            _context.VisitRecords.Add(visit);
            var prescription = new Prescription
            {
                VisitRecord = visit,
                MedicationName = medicationName,
                Dosage = dosage,
                Frequency = frequency,
                Duration = duration
            };

            _context.Prescriptions.Add(prescription);

            // Final workflow status
            appointment.Status = "Completed";

            await _context.SaveChangesAsync();

            await _notificationService.NotifyVisitCompletedAsync(
                appointment,
                visit.Diagnosis);

            TempData["SuccessMessage"] =
                "Visit completed successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ── Doctor adds prescription ───────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> AddPrescription(
    int visitRecordId,
    string medicationName,
    string dosage,
    string frequency,
    string duration)
        {
            var visitRecord = await _context.VisitRecords
                .Include(v => v.Appointment)
                .FirstOrDefaultAsync(v => v.Id == visitRecordId);

            if (visitRecord == null)
            {
                TempData["ErrorMessage"] = "Visit record not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent adding prescription for old visits
            if (visitRecord.VisitDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] =
                    "You cannot add prescriptions to past visits.";

                return RedirectToAction(nameof(Index));
            }

            var prescription = new Prescription
            {
                VisitRecordId = visitRecordId,
                MedicationName = medicationName,
                Dosage = dosage,
                Frequency = frequency,
                Duration = duration
            };

            _context.Prescriptions.Add(prescription);

            await _context.SaveChangesAsync();

            await _notificationService.NotifyPrescriptionAddedAsync(
                visitRecord.Appointment!,
                medicationName);

            TempData["SuccessMessage"] =
                $"Prescription for {medicationName} added successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}