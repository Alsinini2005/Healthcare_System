
using HealthcareClinic.API.Models.Entities;
using HealthcareClinic.API.Models.DTOs;
using System;
using System.Collections.Generic;

namespace Healthcare_System_AdavanceProgrammingProject.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string CurrentUserRole { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;

        // Core dynamic lists for data rendering
        public List<Appointment> ActiveAppointments { get; set; } = new List<Appointment>();
        public List<Doctor> ClinicDoctors { get; set; } = new List<Doctor>();
        public List<Patient> ClinicPatients { get; set; } = new List<Patient>();
        public List<VisitRecord> PastMedicalHistory { get; set; } = new List<VisitRecord>();

        // Carrier object for forms adding records
        public Appointment NewAppointmentPlaceholder { get; set; } = new Appointment();
        public VisitRecord NewVisitPlaceholder { get; set; } = new VisitRecord();
    }
}