using Puerto92.Models;
using System.Collections.Generic;

namespace Puerto92.ViewModels
{
    public class ProveedoresViewModel
    {
        public List<Proveedor> Proveedores { get; set; } = new List<Proveedor>();
        public List<string> Categorias { get; set; } = new List<string>();
        public string SearchTerm { get; set; }
        public string CategoriaSeleccionada { get; set; } = "Todas";
    }
}