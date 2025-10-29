using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    public class UsuarioViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        [Display(Name = "Usuario")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Debe incluir mayúsculas, minúsculas y números")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol")]
        public string RolId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un local")]
        [Display(Name = "Local")]
        public int LocalId { get; set; }

        [Display(Name = "Estado")]
        public bool Activo { get; set; } = true;

        // Propiedades para mostrar en vista
        public string? NombreRol { get; set; }
        public string? NombreLocal { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}