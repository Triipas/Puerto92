using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;

namespace Puerto92.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var rolPrincipal = roles.FirstOrDefault() ?? "Sin rol";

            // Crear ViewModel según el rol
            var viewModel = new DashboardViewModel
            {
                NombreUsuario = user.NombreCompleto,
                UserName = user.UserName ?? "",
                Rol = rolPrincipal,
                LocalNombre = user.Local?.Nombre ?? "N/A",
                UltimoAcceso = user.UltimoAcceso
            };

            // Cargar estadísticas según el rol
            if (User.IsInRole("Admin Maestro"))
            {
                viewModel = await CargarDashboardAdminMaestro(viewModel);
                return View("IndexAdminMaestro", viewModel);
            }
            else if (User.IsInRole("Administrador Local"))
            {
                viewModel = await CargarDashboardAdminLocal(viewModel, user.LocalId);
                return View("IndexAdminLocal", viewModel);
            }
            else if (User.IsInRole("Contador"))
            {
                viewModel = await CargarDashboardContador(viewModel);
                return View("IndexContador", viewModel);
            }
            else if (User.IsInRole("Supervisora de Calidad"))
            {
                viewModel = await CargarDashboardSupervisora(viewModel);
                return View("IndexSupervisora", viewModel);
            }
            else if (User.IsInRole("Jefe de Cocina"))
            {
                viewModel = await CargarDashboardJefeCocina(viewModel, user.LocalId);
                return View("IndexJefeCocina", viewModel);
            }
            else
            {
                // Vista genérica para roles operativos (Mozo, Cocinero, Vajillero)
                return View("IndexGenerico", viewModel);
            }
        }

        // ==========================================
        // DASHBOARDS ESPECÍFICOS POR ROL
        // ==========================================

        /// <summary>
        /// Dashboard para Admin Maestro - Vista completa del sistema
        /// </summary>
        private async Task<DashboardViewModel> CargarDashboardAdminMaestro(DashboardViewModel viewModel)
        {
            viewModel.TotalUsuarios = await _context.Users.CountAsync();
            viewModel.UsuariosActivos = await _context.Users.CountAsync(u => u.Activo);
            viewModel.TotalLocales = await _context.Locales.CountAsync();
            viewModel.LocalesActivos = await _context.Locales.CountAsync(l => l.Activo);
            viewModel.TotalProductos = await _context.Productos.CountAsync();
            viewModel.TotalUtensilios = await _context.Utensilios.CountAsync();
            viewModel.TotalProveedores = await _context.Proveedores.CountAsync(p => p.Activo);

            // Últimos usuarios creados
            viewModel.UltimosUsuarios = await _context.Users
                .OrderByDescending(u => u.FechaCreacion)
                .Take(5)
                .Select(u => new UsuarioResumeViewModel
                {
                    NombreCompleto = u.NombreCompleto,
                    UserName = u.UserName ?? "",
                    FechaCreacion = u.FechaCreacion,
                    Activo = u.Activo
                })
                .ToListAsync();

            return viewModel;
        }

        /// <summary>
        /// Dashboard para Administrador Local - Vista de su local
        /// </summary>
        private async Task<DashboardViewModel> CargarDashboardAdminLocal(DashboardViewModel viewModel, int localId)
        {
            viewModel.TotalUsuarios = await _context.Users.CountAsync(u => u.LocalId == localId);
            viewModel.UsuariosActivos = await _context.Users.CountAsync(u => u.LocalId == localId && u.Activo);

            // Asignaciones pendientes del local
            viewModel.AsignacionesPendientes = await _context.AsignacionesKardex
                .CountAsync(a => a.LocalId == localId && a.Estado == "Pendiente");

            // Kardex del día
            var hoy = DateTime.Today;
            viewModel.KardexHoy = await _context.AsignacionesKardex
                .CountAsync(a => a.LocalId == localId && a.Fecha.Date == hoy);

            return viewModel;
        }

        /// <summary>
        /// Dashboard para Contador - Vista de utensilios
        /// </summary>
        private async Task<DashboardViewModel> CargarDashboardContador(DashboardViewModel viewModel)
        {
            viewModel.TotalUtensilios = await _context.Utensilios.CountAsync();
            viewModel.UtensiliosActivos = await _context.Utensilios.CountAsync(u => u.Activo);
            
            // Valor total del inventario de utensilios
            viewModel.ValorInventarioUtensilios = await _context.Utensilios
                .Where(u => u.Activo)
                .SumAsync(u => u.Precio);

            // Utensilios por tipo
            viewModel.UtensiliosCocina = await _context.Utensilios.CountAsync(u => u.Tipo == "Cocina" && u.Activo);
            viewModel.UtensiliosMozos = await _context.Utensilios.CountAsync(u => u.Tipo == "Mozos" && u.Activo);
            viewModel.UtensiliosVajilla = await _context.Utensilios.CountAsync(u => u.Tipo == "Vajilla" && u.Activo);

            return viewModel;
        }

        /// <summary>
        /// Dashboard para Supervisora de Calidad - Vista de productos y proveedores
        /// </summary>
        private async Task<DashboardViewModel> CargarDashboardSupervisora(DashboardViewModel viewModel)
        {
            viewModel.TotalProductos = await _context.Productos.CountAsync();
            viewModel.ProductosActivos = await _context.Productos.CountAsync(p => p.Activo);
            viewModel.TotalProveedores = await _context.Proveedores.CountAsync();
            viewModel.ProveedoresActivos = await _context.Proveedores.CountAsync(p => p.Activo);

            // Productos por categoría
            viewModel.ProductosBebidas = await _context.Productos
                .CountAsync(p => p.Categoria != null && p.Categoria.Tipo == "Bebidas" && p.Activo);
            viewModel.ProductosCocina = await _context.Productos
                .CountAsync(p => p.Categoria != null && p.Categoria.Tipo == "Cocina" && p.Activo);

            return viewModel;
        }

        /// <summary>
        /// Dashboard para Jefe de Cocina - Vista de sus asignaciones
        /// </summary>
        private async Task<DashboardViewModel> CargarDashboardJefeCocina(DashboardViewModel viewModel, int localId)
        {
            var hoy = DateTime.Today;

            // Asignaciones de hoy
            viewModel.AsignacionesHoy = await _context.AsignacionesKardex
                .CountAsync(a => a.LocalId == localId && a.Fecha.Date == hoy);

            viewModel.AsignacionesPendientes = await _context.AsignacionesKardex
                .CountAsync(a => a.LocalId == localId && a.Estado == "Pendiente" && a.Fecha.Date == hoy);

            viewModel.AsignacionesCompletadas = await _context.AsignacionesKardex
                .CountAsync(a => a.LocalId == localId && a.Estado == "Completada" && a.Fecha.Date == hoy);

            return viewModel;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}