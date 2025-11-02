using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    public class Usuario : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        public int LocalId { get; set; }
        
        public Local? Local { get; set; }

        /// <summary>
        /// Indica si es el primer ingreso del usuario (recién creado, nunca ha iniciado sesión)
        /// </summary>
        public bool EsPrimerIngreso { get; set; } = true;
        
        /// <summary>
        /// Indica si la contraseña fue reseteada por un administrador
        /// </summary>
        public bool PasswordReseteada { get; set; } = false;
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public DateTime? UltimoAcceso { get; set; }
        
        public bool Activo { get; set; } = true;
    }
}