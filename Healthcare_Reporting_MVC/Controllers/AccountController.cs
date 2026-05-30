

using HealthcareClinic.ReportingApp.Models;
using HealthcareClinic.ReportingApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;



namespace HealthcareClinic.ReportingApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ReportingApiService _apiService;

        public AccountController(ReportingApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var result = await _apiService.LoginAsync(model.Email, model.Password);

            if (result == null)
            {
                model.ErrorMessage = "Invalid credentials or you do not have access to this portal.";
                return View(model);
            }

            // Only ClinicManager can access the reporting app
            if (result.Role != "ClinicManager")
            {
                model.ErrorMessage = "Access denied. This portal is restricted to Clinic Managers only.";
                return View(model);
            }

            // Store JWT in session for use in API calls
            HttpContext.Session.SetString("JwtToken", result.Token);

            // Sign in with cookie for MVC authorization
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, result.Username),
                new Claim(ClaimTypes.Role, result.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Dashboard");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");

        }
    }
}