using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using BomProduction.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace BomProduction.EngineService.Repositories
{
    public class BomRepository
    {
        private readonly string _connectionString;

        public BomRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string is missing");
        }

        public async Task<BomItem> GetBomExplosionAsync(Guid rootProductId)
        {
            using IDbConnection db = new SqlConnection(_connectionString);

            // El CTE Mágico: Trae toda la descendencia en un solo viaje
            var sql = @"
            WITH BomCTE AS (
                -- Nivel 1: Hijos directos del producto principal
                SELECT 
                    ParentId, ChildId AS ProductId, Quantity, Notes, 1 AS Level
                FROM BomStructures
                WHERE ParentId = @RootId

                UNION ALL

                -- Recursividad: Los hijos de los hijos
                SELECT 
                    b.ParentId, b.ChildId AS ProductId, b.Quantity, b.Notes, c.Level + 1
                FROM BomStructures b
                INNER JOIN BomCTE c ON b.ParentId = c.ProductId
            )
            -- Juntamos el CTE con la tabla Products para traer nombres y SKUs
            SELECT 
                c.ParentId, c.ProductId, c.Quantity, c.Notes, c.Level,
                p.Name, p.SKU AS PartNumber, p.Unit
            FROM BomCTE c
            INNER JOIN Products p ON c.ProductId = p.Id
            ORDER BY c.Level;
        ";

            var flatBomList = (await db.QueryAsync<BomItem>(sql, new { RootId = rootProductId })).ToList();

            // Si no tiene componentes, regresamos un item vacío
            if (!flatBomList.Any()) return new BomItem { ProductId = rootProductId };

            return BuildBomTree(rootProductId, flatBomList);
        }

        // Método helper para armar el JSON anidado en memoria (Súper rápido)
        private BomItem BuildBomTree(Guid rootId, List<BomItem> flatList)
        {
            // Simulamos el nodo raíz para anclar todo
            var rootNode = new BomItem { ProductId = rootId, Level = 0 };
            var lookup = flatList.ToLookup(x => x.ParentId);

            void AddChildren(BomItem node)
            {
                node.Children = lookup[node.ProductId].ToList();
                foreach (var child in node.Children)
                {
                    AddChildren(child); // Llamada recursiva en memoria
                }
            }

            AddChildren(rootNode);
            return rootNode;
        }
    }
}
