using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Puerto92.Models;
using System.Diagnostics;

namespace Puerto92.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Automáticamente devuelve partial view si es AJAX
            return View();
        }

        public IActionResult Privacy()
        {
            if (User.IsInRole("Admin Maestro"))
            {
                return RedirectToAction("Index", "Categorias");
            }

            return View();
        }

        public IActionResult Configuracion()
        {
            // Nueva página de configuración
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}