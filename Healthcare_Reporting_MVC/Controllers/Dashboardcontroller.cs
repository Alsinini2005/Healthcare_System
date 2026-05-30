using HealthcareClinic.ReportingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareClinic.ReportingApp.Controllers
{
    [Authorize(Roles = "ClinicManager")]
    public class DashboardController : Controller
    {
        private readonly ReportingApiService _apiService;

        public DashboardController(ReportingApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /Dashboard — quick stats overview
        public async Task<IActionResult> Index()
        {
            var stats = await _apiService.GetDashboardStatsAsync();
            if (stats == null)
            {
                TempData["ErrorMessage"] = "Unable to retrieve data from the API. Please try again.";
                return View(stats);
            }
            return View(stats);
        }

        // GET: /Dashboard/OperationalReport — full report with doctor utilization
        public async Task<IActionResult> OperationalReport()
        {
            var report = await _apiService.GetOperationalReportAsync();
            if (report == null)
            {
                TempData["ErrorMessage"] = "Unable to retrieve report data from the API.";
                return View(report);
            }
            return View(report);
        }
    }
}