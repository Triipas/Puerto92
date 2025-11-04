using System;
using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    public class ProductoViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = null!;

        [Required]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = null!; // Solo texto

        [Required]
        [Display(Name = "Unidad de Medida")]
        public string UnidadMedida { get; set; } = null!;

        [Required]
        [Display(Name = "Precio Compra")]
        public decimal PrecioCompra { get; set; }

        [Required]
        [Display(Name = "Precio Venta")]
        public decimal PrecioVenta { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
    }
}