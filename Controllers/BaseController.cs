using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Puerto92.Controllers
{
    /// <summary>
    /// Controlador base que maneja automáticamente peticiones AJAX
    /// devolviendo Partial Views en lugar del layout completo
    /// </summary>
    public class BaseController : Controller
    {
        /// <summary>
        /// Determina si la petición es AJAX
        /// </summary>
        protected bool IsAjaxRequest
        {
            get
            {
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            }
        }

        /// <summary>
        /// Se ejecuta antes de cada acción
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Si es petición AJAX, usar layout parcial o ninguno
            if (IsAjaxRequest)
            {
                ViewData["IsAjax"] = true;
            }
        }

        /// <summary>
        /// Devuelve una vista con soporte automático para AJAX
        /// </summary>
        protected new IActionResult View(string? viewName = null, object? model = null)
        {
            if (IsAjaxRequest)
            {
                // Devolver partial view sin layout
                return base.PartialView(viewName ?? ControllerContext.ActionDescriptor.ActionName, model);
            }

            // Devolver vista completa con layout
            return base.View(viewName, model);
        }

        /// <summary>
        /// Devuelve un resultado JSON con formato estándar
        /// </summary>
        protected JsonResult JsonSuccess(string message = "Operación exitosa", object? data = null, string? redirectUrl = null)
        {
            return Json(new
            {
                success = true,
                message,
                data,
                redirectUrl
            });
        }

        /// <summary>
        /// Devuelve un resultado JSON de error con formato estándar
        /// </summary>
        protected JsonResult JsonError(string message = "Ha ocurrido un error", object? errors = null)
        {
            return Json(new
            {
                success = false,
                message,
                errors
            });
        }

        /// <summary>
        /// Establece un mensaje de éxito en TempData
        /// </summary>
        protected void SetSuccessMessage(string message)
        {
            TempData["Success"] = message;
        }

        /// <summary>
        /// Establece un mensaje de error en TempData
        /// </summary>
        protected void SetErrorMessage(string message)
        {
            TempData["Error"] = message;
        }

        /// <summary>
        /// Redirige con soporte para AJAX
        /// </summary>
        protected IActionResult RedirectToActionAjax(string actionName, string? controllerName = null)
        {
            if (IsAjaxRequest)
            {
                var url = Url.Action(actionName, controllerName);
                return JsonSuccess("Redirigiendo...", redirectUrl: url);
            }

            return RedirectToAction(actionName, controllerName);
        }
    }
}

// ==========================================
// EJEMPLO DE USO EN CONTROLLERS
// ==========================================

/*
// Antes (HomeController tradicional):
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

// Después (HomeController con soporte AJAX automático):
public class HomeController : BaseController
{
    public IActionResult Index()
    {
        // Si es AJAX, devuelve partial view automáticamente
        // Si es navegación normal, devuelve vista completa con layout
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(ItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest)
                return JsonError("Datos inválidos", ModelState);
            
            return View(model);
        }

        // ... lógica de creación ...

        SetSuccessMessage("Item creado exitosamente");
        return RedirectToActionAjax(nameof(Index));
    }
}
*/