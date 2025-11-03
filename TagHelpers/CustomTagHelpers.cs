using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Puerto92.TagHelpers
{
    /// <summary>
    /// Tag Helper para mostrar/ocultar contenido según roles
    /// Uso: <div authorize-roles="Admin Maestro,Contador">...</div>
    /// </summary>
    [HtmlTargetElement(Attributes = "authorize-roles")]
    public class AuthorizeRolesTagHelper : TagHelper
    {
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        [HtmlAttributeName("authorize-roles")]
        public string? Roles { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (ViewContext == null || string.IsNullOrWhiteSpace(Roles))
            {
                output.SuppressOutput();
                return;
            }

            var user = ViewContext.HttpContext.User;
            var allowedRoles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(r => r.Trim())
                                   .ToList();

            bool isAuthorized = allowedRoles.Any(role => user.IsInRole(role));

            if (!isAuthorized)
            {
                output.SuppressOutput();
            }
            else
            {
                // Remover el atributo authorize-roles del HTML final
                output.Attributes.RemoveAll("authorize-roles");
            }
        }
    }

    /// <summary>
    /// Tag Helper para marcar el enlace activo en navegación
    /// Uso: <a asp-controller="Home" asp-action="Index" active-route>Link</a>
    /// </summary>
    [HtmlTargetElement("a", Attributes = "active-route")]
    public class ActiveRouteTagHelper : TagHelper
    {
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        [HtmlAttributeName("asp-controller")]
        public string? Controller { get; set; }

        [HtmlAttributeName("asp-action")]
        public string? Action { get; set; }

        [HtmlAttributeName("active-class")]
        public string ActiveClass { get; set; } = "active";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (ViewContext == null)
                return;

            var currentController = ViewContext.RouteData.Values["controller"]?.ToString();
            var currentAction = ViewContext.RouteData.Values["action"]?.ToString();

            bool isActive = false;

            if (!string.IsNullOrEmpty(Controller))
            {
                isActive = Controller.Equals(currentController, StringComparison.OrdinalIgnoreCase);

                // Si también se especifica action, validar ambos
                if (isActive && !string.IsNullOrEmpty(Action))
                {
                    isActive = Action.Equals(currentAction, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (isActive)
            {
                var existingClasses = output.Attributes["class"]?.Value?.ToString() ?? "";
                output.Attributes.SetAttribute("class", $"{existingClasses} {ActiveClass}".Trim());
            }

            // Remover el atributo active-route del HTML final
            output.Attributes.RemoveAll("active-route");
        }
    }

    /// <summary>
    /// Tag Helper para iconos de Font Awesome
    /// Uso: <icon name="user" /> → <i class="fa-solid fa-user"></i>
    /// </summary>
    [HtmlTargetElement("icon")]
    public class IconTagHelper : TagHelper
    {
        [HtmlAttributeName("name")]
        public string? Name { get; set; }

        [HtmlAttributeName("style")]
        public string Style { get; set; } = "solid"; // solid, regular, brands

        [HtmlAttributeName("size")]
        public string? Size { get; set; }

        [HtmlAttributeName("color")]
        public string? Color { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "i";
            output.TagMode = TagMode.StartTagAndEndTag;

            var classes = $"fa-{Style} fa-{Name}";
            if (!string.IsNullOrEmpty(Size))
                classes += $" fa-{Size}";

            output.Attributes.SetAttribute("class", classes);

            if (!string.IsNullOrEmpty(Color))
            {
                output.Attributes.SetAttribute("style", $"color: {Color}");
            }
        }
    }

    /// <summary>
    /// Tag Helper para badges
    /// Uso: <badge type="success">Activo</badge>
    /// </summary>
    [HtmlTargetElement("badge")]
    public class BadgeTagHelper : TagHelper
    {
        [HtmlAttributeName("type")]
        public string Type { get; set; } = "primary"; // primary, success, danger, warning, info

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.Attributes.SetAttribute("class", $"badge badge-{Type}");
        }
    }

    /// <summary>
    /// Tag Helper para botones con iconos
    /// Uso: <btn-icon icon="edit" type="primary" />
    /// </summary>
    [HtmlTargetElement("btn-icon")]
    public class ButtonIconTagHelper : TagHelper
    {
        [HtmlAttributeName("icon")]
        public string? Icon { get; set; }

        [HtmlAttributeName("type")]
        public string Type { get; set; } = "edit"; // edit, delete, reset, view

        [HtmlAttributeName("title")]
        public string? Title { get; set; }

        [HtmlAttributeName("onclick")]
        public string? OnClick { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "button";
            output.Attributes.SetAttribute("type", "button");
            output.Attributes.SetAttribute("class", $"btn-icon btn-icon-{Type}");

            if (!string.IsNullOrEmpty(Title))
                output.Attributes.SetAttribute("title", Title);

            if (!string.IsNullOrEmpty(OnClick))
                output.Attributes.SetAttribute("onclick", OnClick);

            var iconClass = Icon ?? Type switch
            {
                "edit" => "pen-to-square",
                "delete" => "trash-can",
                "reset" => "rotate-right",
                "view" => "eye",
                _ => "circle"
            };

            var iconStyle = Type switch
            {
                "delete" => "regular",
                "edit" => "regular",
                _ => "solid"
            };

            output.Content.SetHtmlContent($"<i class=\"fa-{iconStyle} fa-{iconClass}\"></i>");
        }
    }
}