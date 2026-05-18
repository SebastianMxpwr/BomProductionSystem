using System;
using System.Collections.Generic;
using System.Text;

namespace BomProduction.Shared.Models
{
    public class BomItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ParentId { get; set; } // Null si es el producto raíz
        public string PartNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int Level { get; set; } // Profundidad en el árbol
        public string? Notes { get; set; }

        // Lista recursiva para la vista del front
        public List<BomItem> Children { get; set; } = new();
    }
}
