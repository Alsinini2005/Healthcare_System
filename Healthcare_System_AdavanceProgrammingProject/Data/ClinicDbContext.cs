using Microsoft.EntityFrameworkCore;
using HealthcareClinic.API.Models.Entities;

namespace HealthcareClinic.API.Data
{
    public class ClinicDbContext : DbContext
    {
        public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

        // Core tables
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<DoctorSpecialization> DoctorSpecializations { get; set; }

        // Interaction tables
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<VisitRecord> VisitRecords { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }

        // System table
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Composite key for the many-to-many join table ──────────────────
            modelBuilder.Entity<DoctorSpecialization>()
                .HasKey(ds => new { ds.DoctorId, ds.SpecializationId });

            modelBuilder.Entity<DoctorSpecialization>()
                .HasOne(ds => ds.Doctor)
                .WithMany(d => d.DoctorSpecializations)
                .HasForeignKey(ds => ds.DoctorId);

            modelBuilder.Entity<DoctorSpecialization>()
                .HasOne(ds => ds.Specialization)
                .WithMany(s => s.DoctorSpecializations)
                .HasForeignKey(ds => ds.SpecializationId);

            // ── Patient → User (one-to-one) ────────────────────────────────────
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Doctor → User (one-to-one) ─────────────────────────────────────
            // FIX #1: Use HasOne/WithOne so every Doctor row MUST have a User row.
            // This prevents doctors from appearing in Users but not in Doctors.
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithOne(u => u.DoctorProfile)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Appointment → Patient ──────────────────────────────────────────
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Appointment → Doctor ───────────────────────────────────────────
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── VisitRecord → Appointment ──────────────────────────────────────
            modelBuilder.Entity<VisitRecord>()
                .HasOne(v => v.Appointment)
                .WithOne(a => a.VisitRecord)
                .HasForeignKey<VisitRecord>(v => v.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Prescription → VisitRecord ─────────────────────────────────────
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.VisitRecord)
                .WithMany(v => v.Prescriptions)
                .HasForeignKey(p => p.VisitRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Notification → User ────────────────────────────────────────────
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ══════════════════════════════════════════════════════════════════
            // SEED DATA
            // All seeded users have password: password123
            const string hashedPassword = "$2a$11$JuK.0gfwfl//T8amWFACiuCqSXEEAixxeJTz0OACExeoQvNf.W/3O";
            // ══════════════════════════════════════════════════════════════════

            modelBuilder.Entity<User>().HasData(
    new User
    {
        Id = 1,
        Name = "Hadi Al-Mansoori",
        Email = "hadi@clinic.com",
        PasswordHash = hashedPassword,
        Role = "ClinicManager",
        IsActive = true,
        CPR = 123456789
    },
    new User
    {
        Id = 2,
        Name = "Dr. Sarah Ahmed",
        Email = "sarah.ahmed@clinic.com",
        PasswordHash = hashedPassword,
        Role = "Doctor",
        IsActive = true,
        CPR = 234567891
    },
    new User
    {
        Id = 3,
        Name = "Dr. Khalid Nasser",
        Email = "khalid.nasser@clinic.com",
        PasswordHash = hashedPassword,
        Role = "Doctor",
        IsActive = true,
        CPR = 345678912
    },
    new User
    {
        Id = 4,
        Name = "Fatima Al-Zayed",
        Email = "fatima@example.com",
        PasswordHash = hashedPassword,
        Role = "Receptionist",
        IsActive = true,
        CPR = 456789123
    },
    new User
    {
        Id = 5,
        Name = "Ali Mansoor",
        Email = "ali@example.com",
        PasswordHash = hashedPassword,
        Role = "Patient",
        IsActive = true,
        CPR = 567891234
    },
    new User
    {
        Id = 6,
        Name = "Mariam Hassan",
        Email = "mariam@example.com",
        PasswordHash = hashedPassword,
        Role = "Patient",
        IsActive = true,
        CPR = 678912345
    }

            );

            // 2. Doctors (UserId links to User table 1-to-1)
            // FIX #1: Every Doctor row maps to exactly one User with Role="Doctor".
            // When creating a new doctor via the UI, always create the User first,
            // then the Doctor row with that UserId. This guarantees the full list
            // appears in the Clinic Manager dashboard.
            modelBuilder.Entity<Doctor>().HasData(
            new Doctor
            {
                Id = 1,
                UserId = 2,
                Name = "Dr. Sarah Ahmed",
                Email = "sarah.ahmed@clinic.com",
                WorkingHours = "08:00 - 16:00",
                DaysOff = "Friday, Saturday",
                CPR = 234567891
            },
            new Doctor
            {
                Id = 2,
                UserId = 3,
                Name = "Dr. Khalid Nasser",
                Email = "khalid.nasser@clinic.com",
                WorkingHours = "09:00 - 17:00",
                DaysOff = "Friday, Saturday",
                CPR = 345678912
            }
        );

            // 3. Patients (UserId links to User table)
            modelBuilder.Entity<Patient>().HasData(
                new Patient
                {
                    Id = 1,
                    UserId = 5,
                    Name = "Ali Mansoor",
                    CPRNumber = "990123456",
                    PatientReferenceNumber = "REF-2026-ALI"
                },
                new Patient
                {
                    Id = 2,
                    UserId = 6,
                    Name = "Mariam Hassan",
                    CPRNumber = "880234567",
                    PatientReferenceNumber = "REF-2026-MAR"
                }
            );

            // 4. Specializations
            modelBuilder.Entity<Specialization>().HasData(
                new Specialization
                {
                    Id = 1,
                    Name = "Cardiology",
                    Description = "Heart and cardiovascular care."
                },
                new Specialization
                {
                    Id = 2,
                    Name = "General Practice",
                    Description = "General health checkups and common illness treatment."
                }
            );

            // 5. DoctorSpecializations (join table — many-to-many)
            // FIX #2: The join table correctly handles the many-to-many relationship.
            // A doctor can have multiple specializations and a specialization can
            // belong to multiple doctors. Add more rows here as needed.
            modelBuilder.Entity<DoctorSpecialization>().HasData(
                new DoctorSpecialization { DoctorId = 1, SpecializationId = 1 }, // Sarah → Cardiology
                new DoctorSpecialization { DoctorId = 2, SpecializationId = 2 }  // Khalid → General Practice
            );

            // 6. Appointments
            modelBuilder.Entity<Appointment>().HasData(
                new Appointment
                {
                    Id = 1,
                    PatientId = 1,
                    DoctorId = 1,
                    AppointmentDate = new DateTime(2026, 6, 1, 9, 0, 0),
                    Status = "Completed",
                    Specialty = "Cardiology",
                    DoctorNotes = "Routine cardiovascular follow-up."
                },
                new Appointment
                {
                    Id = 2,
                    PatientId = 2,
                    DoctorId = 2,
                    AppointmentDate = new DateTime(2026, 6, 3, 10, 0, 0),
                    Status = "Confirmed",
                    Specialty = "General Practice",
                    DoctorNotes = "First general checkup visit."
                },
                new Appointment
                {
                    Id = 3,
                    PatientId = 1,
                    DoctorId = 2,
                    AppointmentDate = new DateTime(2026, 6, 10, 11, 0, 0),
                    Status = "Requested",
                    Specialty = "General Practice",
                    DoctorNotes = ""
                }
            );

            // 7. VisitRecord for the completed appointment
            modelBuilder.Entity<VisitRecord>().HasData(
                new VisitRecord
                {
                    Id = 1,
                    AppointmentId = 1,
                    VisitDate = new DateTime(2026, 6, 1, 9, 30, 0),
                    Diagnosis = "Mild hypertension detected. Monitoring recommended.",
                    DoctorNotes = "Patient advised to reduce salt intake and exercise regularly.",
                    PrescribedTreatment = "Lifestyle changes and follow-up in 4 weeks."
                }
            );

            // 8. Prescription for the visit record
            modelBuilder.Entity<Prescription>().HasData(
                new Prescription
                {
                    Id = 1,
                    VisitRecordId = 1,
                    MedicationName = "Amlodipine",
                    Dosage = "5mg",
                    Frequency = "Once daily",
                    Duration = "30 days"
                }
            );
        }
    }
}
