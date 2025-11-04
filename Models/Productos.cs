using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Puerto92.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = null!;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        [StringLength(100)]
        public string Categoria { get; set; } = null!; // Solo texto

        [Required]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = null!; // Ej: Unidad, kg, litro

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioCompra { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioVenta { get; set; }

        public bool Activo { get; set; } = true;

        // Timestamps opcionales
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }
    }
}
