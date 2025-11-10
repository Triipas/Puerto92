using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Kardex Pendientes de Revisión
    /// </summary>
    public class KardexPendientesViewModel
    {
        public List<KardexPendienteItem> KardexCocina { get; set; } = new();
        public List<KardexPendienteItem> KardexMozos { get; set; } = new();
        public List<KardexPendienteItem> KardexVajilla { get; set; } = new();
        
        public int TotalPendientesCocina { get; set; }
        public int TotalPendientesMozos { get; set; }
        public int TotalPendientesVajilla { get; set; }
    }

    /// <summary>
    /// Item individual de kardex pendiente
    /// </summary>
    public class KardexPendienteItem
    {
        public int KardexId { get; set; }
        public string TipoKardex { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        
        /// <summary>
        /// Lista de nombres de responsables
        /// Para cocina: 3 cocineros consolidados
        /// Para mozos y vajilla: 1 responsable
        /// </summary>
        public List<string> Responsables { get; set; } = new();
        
        /// <summary>
        /// Cantidad de personal que estuvo presente
        /// </summary>
        public int CantidadPersonalPresente { get; set; }
        
        /// <summary>
        /// Estado del kardex: "Enviado", "En Revisión", "Aprobado", "Rechazado"
        /// </summary>
        public string Estado { get; set; } = "Enviado";
        
        /// <summary>
        /// Para Mozos: "Salón" o "Bebidas"
        /// Para Cocina: "Cocina Fría", "Cocina Caliente", "Parrilla"
        /// Para Vajilla: null
        /// </summary>
        public string? TipoDetalle { get; set; }
        
        /// <summary>
        /// IDs de los kardex agrupados (para cocina)
        /// </summary>
        public List<int> KardexIdsAgrupados { get; set; } = new();
        
        /// <summary>
        /// Indica si este item es una agrupación de múltiples kardex
        /// </summary>
        public bool EsAgrupacion { get; set; } = false;
    }
}