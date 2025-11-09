using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para registrar el personal que estuvo presente durante un kardex
    /// </summary>
    public class PersonalPresente
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del kardex al que pertenece (genérico)
        /// </summary>
        [Required]
        public int KardexId { get; set; }

        /// <summary>
        /// Tipo de kardex (Bebidas, Salón, Cocina, Vajilla)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TipoKardex { get; set; } = string.Empty;

        /// <summary>
        /// ID del empleado que estuvo presente
        /// </summary>
        [Required]
        public string EmpleadoId { get; set; } = string.Empty;

        /// <summary>
        /// Navegación al empleado
        /// </summary>
        public Usuario? Empleado { get; set; }

        /// <summary>
        /// Fecha y hora de registro
        /// </summary>
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si es el responsable principal (quien llenó el kardex)
        /// </summary>
        public bool EsResponsablePrincipal { get; set; } = false;

        /// <summary>
        /// Observaciones adicionales
        /// </summary>
        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}