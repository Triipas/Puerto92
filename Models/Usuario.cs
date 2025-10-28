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

        public bool EsPrimerIngreso { get; set; } = true;
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public DateTime? UltimoAcceso { get; set; }
        
        public bool Activo { get; set; } = true;
    }
}