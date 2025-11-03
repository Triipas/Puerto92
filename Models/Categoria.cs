using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para categorías de productos y utensilios
    /// </summary>
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tipo de categoría: Bebidas, Cocina, Utensilios
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la categoría
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Orden de visualización (menor = primero)
        /// </summary>
        public int Orden { get; set; }

        /// <summary>
        /// Indica si la categoría está activa
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Fecha de creación
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Usuario que creó la categoría
        /// </summary>
        [StringLength(100)]
        public string? CreadoPor { get; set; }

        // Propiedades de navegación (para cuando se implementen productos)
        // public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    /// <summary>
    /// Tipos de categorías permitidos
    /// </summary>
    public static class TipoCategoria
    {
        public const string Bebidas = "Bebidas";
        public const string Cocina = "Cocina";
        public const string Utensilios = "Utensilios";

        public static readonly string[] Todos = new[] { Bebidas, Cocina, Utensilios };

        public static bool EsValido(string tipo)
        {
            return Todos.Contains(tipo);
        }
    }
}