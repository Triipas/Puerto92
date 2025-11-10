using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para el registro de kardex de vajilla y utensilios de cocina
    /// </summary>
    public class KardexVajilla
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID de la asignación relacionada
        /// </summary>
        [Required]
        public int AsignacionId { get; set; }
        public AsignacionKardex? Asignacion { get; set; }

        /// <summary>
        /// Fecha del kardex
        /// </summary>
        [Required]
        public DateTime Fecha { get; set; }

        /// <summary>
        /// ID del local
        /// </summary>
        [Required]
        public int LocalId { get; set; }
        public Local? Local { get; set; }

        /// <summary>
        /// ID del empleado responsable (Vajillero)
        /// </summary>
        [Required]
        public string EmpleadoId { get; set; } = string.Empty;
        public Usuario? Empleado { get; set; }

        /// <summary>
        /// Estado del kardex: Borrador, Completado, Enviado, Aprobado, Rechazado
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Borrador";

        /// <summary>
        /// Fecha de inicio del registro
        /// </summary>
        public DateTime? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de finalización del registro
        /// </summary>
        public DateTime? FechaFinalizacion { get; set; }

        /// <summary>
        /// Fecha de envío al supervisor
        /// </summary>
        public DateTime? FechaEnvio { get; set; }

        /// <summary>
        /// Descripción de faltantes (utensilios rotos, extraviados, etc.)
        /// </summary>
        [StringLength(2000)]
        public string? DescripcionFaltantes { get; set; }

        /// <summary>
        /// Cantidad de utensilios rotos
        /// </summary>
        public int? CantidadRotos { get; set; }

        /// <summary>
        /// Cantidad de utensilios extraviados
        /// </summary>
        public int? CantidadExtraviados { get; set; }

        /// <summary>
        /// Observaciones generales del kardex
        /// </summary>
        public string? Observaciones { get; set; }

        /// <summary>
        /// Detalle de utensilios del kardex
        /// </summary>
        public ICollection<KardexVajillaDetalle> Detalles { get; set; } = new List<KardexVajillaDetalle>();
    }

    /// <summary>
    /// Detalle de cada utensilio en el kardex de vajilla
    /// </summary>
    public class KardexVajillaDetalle
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del kardex padre
        /// </summary>
        [Required]
        public int KardexVajillaId { get; set; }
        public KardexVajilla? KardexVajilla { get; set; }

        /// <summary>
        /// ID del utensilio (de la tabla Utensilios, categorías: Utensilios Cocina, Menajería Cocina, Equipos)
        /// </summary>
        [Required]
        public int UtensilioId { get; set; }
        public Utensilio? Utensilio { get; set; }

        /// <summary>
        /// Inventario inicial esperado
        /// </summary>
        [Required]
        public int InventarioInicial { get; set; }

        /// <summary>
        /// Unidades contadas por el vajillero
        /// </summary>
        public int? UnidadesContadas { get; set; }

        /// <summary>
        /// Diferencia (negativo = faltante, positivo = sobrante)
        /// </summary>
        public int Diferencia { get; set; }

        /// <summary>
        /// Indica si tiene faltantes
        /// </summary>
        public bool TieneFaltantes { get; set; }

        /// <summary>
        /// Observaciones específicas del utensilio
        /// </summary>
        public string? Observaciones { get; set; }

        /// <summary>
        /// Orden de visualización
        /// </summary>
        public int Orden { get; set; }
    }
}