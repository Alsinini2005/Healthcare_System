
using Healthcare_System_AdavanceProgrammingProject.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    // This lives in your MVC project and serves the HTML views to the patient
    public class PublicLookupController : Controller
    {
        private readonly ClinicApiService _apiService;

        public PublicLookupController(ClinicApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /PublicLookup
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /PublicLookup/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string cprNumber, string patientRefNumber)
        {
            if (string.IsNullOrWhiteSpace(cprNumber) || string.IsNullOrWhiteSpace(patientRefNumber))
            {
                ViewBag.ErrorMessage = "Please provide both your CPR Number and Patient Reference Number.";
                return View("Index");
            }

            // This hits your Web API project over the network!
            var appointments = await _apiService.LookupAppointmentsAsync(cprNumber, patientRefNumber);

            if (appointments == null)
            {
                ViewBag.ErrorMessage = "No clinic records matched the provided criteria. Check your reference values.";
                return View("Index");
            }

            return View("Results", appointments);
        }
    }
}
