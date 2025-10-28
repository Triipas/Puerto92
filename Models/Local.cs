using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    public class Local
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Distrito { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}