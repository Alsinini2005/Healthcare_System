using Healthcare_MVC.Models;
using Healthcare_System_AdavanceProgrammingProject.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    public class HomeController : Controller
    {
        // GET: / (Main landing page)
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
