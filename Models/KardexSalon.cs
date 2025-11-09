using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para el registro de kardex de utensilios de salón
    /// </summary>
    public class KardexSalon
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
        /// ID del local - REQUERIDO
        /// </summary>
        [Required]
        public int LocalId { get; set; }
        public Local? Local { get; set; }

        /// <summary>
        /// ID del empleado responsable (mozo)
        /// </summary>
        [Required]
        [StringLength(450)]
        public string EmpleadoId { get; set; } = string.Empty;
        public Usuario? Empleado { get; set; }

        /// <summary>
        /// Estado del kardex: Borrador, Enviado, Aprobado, Rechazado
        /// </summary>
        [Required]
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
        /// Observaciones generales del kardex
        /// </summary>
        public string? Observaciones { get; set; }

        /// <summary>
        /// Detalle de utensilios del kardex
        /// </summary>
        public ICollection<KardexSalonDetalle> Detalles { get; set; } = new List<KardexSalonDetalle>();
    }

    /// <summary>
    /// Detalle de cada utensilio en el kardex de salón
    /// </summary>
    public class KardexSalonDetalle
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del kardex padre
        /// </summary>
        [Required]
        public int KardexSalonId { get; set; }
        public KardexSalon? KardexSalon { get; set; }

        /// <summary>
        /// ID del utensilio (de la tabla Utensilios)
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
        /// Unidades contadas por el mozo
        /// </summary>
        public int? UnidadesContadas { get; set; }

        /// <summary>
        /// Diferencia calculada (Inventario Inicial - Unidades Contadas)
        /// </summary>
        public int Diferencia { get; set; }

        /// <summary>
        /// Indica si tiene faltantes que requieren justificación
        /// </summary>
        public bool TieneFaltantes { get; set; }

        /// <summary>
        /// Descripción de los faltantes (rotos, extraviados, etc.)
        /// </summary>
        public string? DescripcionFaltantes { get; set; }

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