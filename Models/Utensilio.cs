using System;
using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    public class Utensilio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } // Cocina / Mozos / Vajilla

        [Required]
        [StringLength(20)]
        public string Unidad { get; set; } // Unidad, Caja, etc.

        [Required]
        [DataType(DataType.Currency)]
        public decimal Precio { get; set; }

        [StringLength(250)]
        public string Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        // Fecha de creación y actualización opcional
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }
    }
}