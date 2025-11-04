using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener 11 dígitos")]
        public string Ruc { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(50)]
        public string Categoria { get; set; }  // Texto directo, no tabla relacionada

        [StringLength(20)]
        public string Telefono { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string PersonaContacto { get; set; }

        [StringLength(200)]
        public string Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}