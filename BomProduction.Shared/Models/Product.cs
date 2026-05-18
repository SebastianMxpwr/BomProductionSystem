using System;
using System.Collections.Generic;
using System.Text;

namespace BomProduction.Shared.Models
{
    public class Product
    {
        public Guid Id { get; set; } // Empata con "ABC123" pero en formato UUID
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
