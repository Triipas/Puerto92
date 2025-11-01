using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Puerto92.Models;
using Puerto92.ViewModels;

namespace Puerto92.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<Usuario> signInManager,
            UserManager<Usuario> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Si ya está autenticado, redirigir al dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Intentar iniciar sesión
            var result = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null)
                {
                    // Actualizar último acceso
                    user.UltimoAcceso = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation($"Usuario {user.UserName} inició sesión correctamente");

                    // Verificar si debe cambiar contraseña (primer ingreso o reseteo)
                    if (user.EsPrimerIngreso || user.PasswordReseteada)
                    {
                        if (user.EsPrimerIngreso)
                        {
                            _logger.LogInformation($"Usuario {user.UserName} - Primer ingreso detectado");
                        }
                        else if (user.PasswordReseteada)
                        {
                            _logger.LogInformation($"Usuario {user.UserName} - Contraseña reseteada, debe cambiarla");
                        }
                        return RedirectToAction(nameof(ChangePassword));
                    }

                    // Redirigir según returnUrl o al Home
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"Cuenta bloqueada: {model.UserName}");
                ModelState.AddModelError(string.Empty, "Cuenta bloqueada por múltiples intentos fallidos. Intente nuevamente en 15 minutos.");
                return View(model);
            }

            // Login fallido
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuario cerró sesión");
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        // GET: /Account/ChangePassword
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Pasar información a la vista sobre el tipo de cambio requerido
            ViewBag.EsPrimerIngreso = user.EsPrimerIngreso;
            ViewBag.PasswordReseteada = user.PasswordReseteada;

            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.EsPrimerIngreso = user.EsPrimerIngreso;
                    ViewBag.PasswordReseteada = user.PasswordReseteada;
                }
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(currentUser, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Limpiar ambas banderas después del cambio exitoso
                currentUser.EsPrimerIngreso = false;
                currentUser.PasswordReseteada = false;
                await _userManager.UpdateAsync(currentUser);

                _logger.LogInformation($"Usuario {currentUser.UserName} cambió su contraseña exitosamente");

                // Re-autenticar al usuario
                await _signInManager.RefreshSignInAsync(currentUser);

                TempData["Success"] = "Contraseña cambiada exitosamente";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Volver a cargar información para la vista
            ViewBag.EsPrimerIngreso = currentUser.EsPrimerIngreso;
            ViewBag.PasswordReseteada = currentUser.PasswordReseteada;

            return View(model);
        }

    }
}