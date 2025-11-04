using System;
using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    public class UtensilioViewModel
    {
        // Id opcional, no lo necesitamos en la creación
        public int Id { get; set; }

        [Display(Name = "Código")]
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(50)]
        public string Codigo { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Display(Name = "Tipo")]
        [Required(ErrorMessage = "El tipo es obligatorio")]
        [StringLength(20)]
        public string Tipo { get; set; } // Cocina / Mozos / Vajilla

        [Display(Name = "Unidad")]
        [Required(ErrorMessage = "La unidad es obligatoria")]
        [StringLength(20)]
        public string Unidad { get; set; } // Unidad, Set, Caja, etc.

        [Display(Name = "Precio")]
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 1000000, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(250)]
        public string Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // Campos opcionales de auditoría
        [Display(Name = "Fecha Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Fecha Actualización")]
        public DateTime? FechaActualizacion { get; set; }
    }
}