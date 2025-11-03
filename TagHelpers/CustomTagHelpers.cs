using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Puerto92.TagHelpers
{
    // ==========================================
    // 🔒 AUTORIZACIÓN POR ROLES
    // ==========================================
    /// <summary>
    /// Uso: <div authorize-roles="Admin Maestro,Contador">Contenido</div>
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
                output.Attributes.RemoveAll("authorize-roles");
            }
        }
    }

    // ==========================================
    // 🎯 NAVEGACIÓN ACTIVA
    // ==========================================
    /// <summary>
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
            if (ViewContext == null) return;

            var currentController = ViewContext.RouteData.Values["controller"]?.ToString();
            var currentAction = ViewContext.RouteData.Values["action"]?.ToString();

            bool isActive = false;

            if (!string.IsNullOrEmpty(Controller))
            {
                isActive = Controller.Equals(currentController, StringComparison.OrdinalIgnoreCase);

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

            output.Attributes.RemoveAll("active-route");
        }
    }

    // ==========================================
    // 🎨 ICONOS FONT AWESOME
    // ==========================================
    /// <summary>
    /// Uso Simple: <icon name="user" />
    /// Uso Avanzado: <icon name="user" size="2x" color="#0092B8" />
    /// </summary>
    [HtmlTargetElement("icon")]
    public class IconTagHelper : TagHelper
    {
        [HtmlAttributeName("name")]
        public string? Name { get; set; }

        [HtmlAttributeName("style")]
        public string Style { get; set; } = "solid"; // solid, regular, brands

        [HtmlAttributeName("size")]
        public string? Size { get; set; } // sm, lg, 2x, 3x, etc.

        [HtmlAttributeName("color")]
        public string? Color { get; set; }

        [HtmlAttributeName("spin")]
        public bool Spin { get; set; } = false;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "i";
            output.TagMode = TagMode.StartTagAndEndTag;

            // Clases base
            var classes = $"fa-{Style} fa-{Name}";
            
            // Tamaño
            if (!string.IsNullOrEmpty(Size))
                classes += $" fa-{Size}";
            
            // Spin
            if (Spin)
                classes += " fa-spin";

            output.Attributes.SetAttribute("class", classes);

            // Color
            if (!string.IsNullOrEmpty(Color))
            {
                output.Attributes.SetAttribute("style", $"color: {Color}");
            }
        }
    }

    // ==========================================
    // 🏷️ BADGES/ETIQUETAS
    // ==========================================
    /// <summary>
    /// Uso: <badge type="success">Activo</badge>
    /// Tipos: primary, success, danger, warning, info
    /// </summary>
    [HtmlTargetElement("badge")]
    public class BadgeTagHelper : TagHelper
    {
        [HtmlAttributeName("type")]
        public string Type { get; set; } = "primary";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.Attributes.SetAttribute("class", $"badge badge-{Type}");
        }
    }

    // ==========================================
    // 🔘 BOTONES CON ICONOS (CRUD)
    // ==========================================
    /// <summary>
    /// Uso: <btn-icon type="edit" title="Editar" onclick="editarItem()" />
    /// Tipos: edit, delete, reset, view, add, save, cancel
    /// </summary>
    [HtmlTargetElement("btn-icon")]
    public class ButtonIconTagHelper : TagHelper
    {
        [HtmlAttributeName("type")]
        public string Type { get; set; } = "edit";

        [HtmlAttributeName("title")]
        public string? Title { get; set; }

        [HtmlAttributeName("onclick")]
        public string? OnClick { get; set; }

        [HtmlAttributeName("disabled")]
        public bool Disabled { get; set; } = false;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "button";
            output.Attributes.SetAttribute("type", "button");
            output.Attributes.SetAttribute("class", $"btn-icon btn-icon-{Type}");

            if (!string.IsNullOrEmpty(Title))
                output.Attributes.SetAttribute("title", Title);

            if (!string.IsNullOrEmpty(OnClick))
                output.Attributes.SetAttribute("onclick", OnClick);

            if (Disabled)
                output.Attributes.SetAttribute("disabled", "disabled");

            // Mapeo de iconos según tipo
            var (iconClass, iconStyle) = Type.ToLower() switch
            {
                "edit" => ("pen-to-square", "regular"),
                "delete" => ("trash-can", "regular"),
                "reset" => ("rotate-right", "solid"),
                "view" => ("eye", "solid"),
                "add" => ("plus", "solid"),
                "save" => ("floppy-disk", "regular"),
                "cancel" => ("xmark", "solid"),
                "search" => ("magnifying-glass", "solid"),
                "download" => ("download", "solid"),
                "upload" => ("upload", "solid"),
                "print" => ("print", "solid"),
                _ => ("circle", "solid")
            };

            output.Content.SetHtmlContent($"<i class=\"fa-{iconStyle} fa-{iconClass}\"></i>");
        }
    }

    // ==========================================
    // 📦 CARD CONTAINER
    // ==========================================
    /// <summary>
    /// Uso: <card title="Mi Título" subtitle="Subtítulo opcional">Contenido</card>
    /// </summary>
    [HtmlTargetElement("card")]
    public class CardTagHelper : TagHelper
    {
        [HtmlAttributeName("title")]
        public string? Title { get; set; }

        [HtmlAttributeName("subtitle")]
        public string? Subtitle { get; set; }

        [HtmlAttributeName("icon")]
        public string? Icon { get; set; }

        [HtmlAttributeName("no-padding")]
        public bool NoPadding { get; set; } = false;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "content-card");

            var childContent = await output.GetChildContentAsync();
            var content = childContent.GetContent();

            var html = "";

            // Header si tiene título
            if (!string.IsNullOrEmpty(Title))
            {
                html += "<div class='card-header'>";
                html += "<div>";
                
                if (!string.IsNullOrEmpty(Icon))
                {
                    html += $"<i class='fa-solid fa-{Icon}' style='margin-right: 0.5rem;'></i>";
                }
                
                html += $"<h2 class='card-title'>{Title}</h2>";
                
                if (!string.IsNullOrEmpty(Subtitle))
                {
                    html += $"<p class='card-subtitle'>{Subtitle}</p>";
                }
                
                html += "</div>";
                html += "</div>";
            }

            // Body
            var bodyClass = NoPadding ? "card-body-no-padding" : "card-body";
            html += $"<div class='{bodyClass}'>{content}</div>";

            output.Content.SetHtmlContent(html);
        }
    }

    // ==========================================
    // 🔍 SEARCH BOX
    // ==========================================
    /// <summary>
    /// Uso: <search-box table-id="usuariosTable" placeholder="Buscar usuarios..." />
    /// </summary>
    [HtmlTargetElement("search-box")]
    public class SearchBoxTagHelper : TagHelper
    {
        [HtmlAttributeName("table-id")]
        public string TableId { get; set; } = "dataTable";

        [HtmlAttributeName("placeholder")]
        public string Placeholder { get; set; } = "Buscar...";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "search-box-container");

            var html = $@"
                <div class='search-box'>
                    <i class='fa-solid fa-magnifying-glass'></i>
                    <input type='text' 
                           id='searchInput' 
                           class='form-control search-input' 
                           placeholder='{Placeholder}' 
                           data-table='{TableId}' />
                </div>";

            output.Content.SetHtmlContent(html);
        }
    }
}