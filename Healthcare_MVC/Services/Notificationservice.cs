using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;

namespace HealthcareClinic.API.Services
{
    public class NotificationService
    {
        private readonly ClinicDbContext _context;

        public NotificationService(ClinicDbContext context)
        {
            _context = context;
        }

        // Send a notification to a specific user by their UserId
        public async Task SendAsync(int userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Notify both patient and doctor when an appointment is booked
        public async Task NotifyAppointmentBookedAsync(Appointment appointment)
        {
            var patient = await _context.Patients.FindAsync(appointment.PatientId);
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

            if (patient != null)
                await SendAsync(patient.UserId,
                    $"Your appointment with Dr. {doctor?.Name} on {appointment.AppointmentDate:f} has been requested.");

            if (doctor != null)
                await SendAsync(doctor.UserId,
                    $"New appointment requested by {patient?.Name} on {appointment.AppointmentDate:f}.");
        }

        // Notify patient when appointment status changes
        public async Task NotifyStatusChangedAsync(Appointment appointment)
        {
            var patient = await _context.Patients.FindAsync(appointment.PatientId);
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

            if (patient != null)
                await SendAsync(patient.UserId,
                    $"Your appointment with Dr. {doctor?.Name} on {appointment.AppointmentDate:f} is now {appointment.Status}.");
        }

        // Notify patient and doctor when visit is completed
        public async Task NotifyVisitCompletedAsync(Appointment appointment, string diagnosis)
        {
            var patient = await _context.Patients.FindAsync(appointment.PatientId);
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

            if (patient != null)
                await SendAsync(patient.UserId,
                    $"Your visit with Dr. {doctor?.Name} is complete. Diagnosis: {diagnosis}.");

            if (doctor != null)
                await SendAsync(doctor.UserId,
                    $"Visit record for {patient?.Name} has been saved successfully.");
        }

        // Notify patient when a prescription is added
        public async Task NotifyPrescriptionAddedAsync(Appointment appointment, string medicationName)
        {
            var patient = await _context.Patients.FindAsync(appointment.PatientId);
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

            if (patient != null)
                await SendAsync(patient.UserId,
                    $"Dr. {doctor?.Name} has prescribed {medicationName} for you.");
        }
    }
}