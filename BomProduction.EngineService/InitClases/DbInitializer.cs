using Microsoft.Data.SqlClient;
using Dapper;

namespace BomProduction.EngineService.InitClases
{
    public static class DbInitializer
    {
        public static void Initialize(string connectionString)
        {
            // 1. Nos conectamos a 'master' primero para ver si BOM existe
            var masterConnString = connectionString.Replace("Database=BOM;", "Database=master;");
            using var masterDb = new SqlConnection(masterConnString);

            var checkDbQuery = "SELECT 1 FROM sys.databases WHERE name = 'BOM'";
            
            var exists = masterDb.ExecuteScalar<int?>(checkDbQuery);

            if (exists == null)
            {
                // La BD no existe, la creamos
                masterDb.Execute("CREATE DATABASE BOM");

                // 2. Nos conectamos a la nueva BD y creamos las tablas
                using var bomDb = new SqlConnection(connectionString);

                var initScript = @"
                CREATE TABLE Users (
                    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    Name NVARCHAR(150) NOT NULL,
                    Email NVARCHAR(150) UNIQUE NOT NULL,
                    PasswordHash NVARCHAR(255) NOT NULL, 
                    Role VARCHAR(50) NOT NULL,       -- 'admin', 'supervisor', 'worker'
                    AvatarUrl NVARCHAR(500) NULL,
                    Active BIT DEFAULT 1,
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
                );
                
                CREATE TABLE AppViews (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    ViewName VARCHAR(100) NOT NULL,  
                    Description NVARCHAR(200) NULL
                );
                
                CREATE TABLE RoleViews (
                    Role VARCHAR(50) NOT NULL,
                    ViewId INT NOT NULL,
                    CONSTRAINT PK_RoleViews PRIMARY KEY (Role, ViewId),
                    CONSTRAINT FK_RoleViews_Views FOREIGN KEY (ViewId) REFERENCES AppViews(Id)
                );
                
                
                CREATE TABLE Products (
                    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    SKU VARCHAR(50) UNIQUE NOT NULL,
                    Name NVARCHAR(150) NOT NULL,
                    Description NVARCHAR(MAX) NULL,
                    Category VARCHAR(100) NOT NULL,
                    Unit VARCHAR(20) NOT NULL,       
                    Stock DECIMAL(18,4) DEFAULT 0,
                    ImageUrl NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
                );
                
                
                CREATE TABLE BomStructures (
                    ParentId UNIQUEIDENTIFIER NOT NULL, 
                    ChildId UNIQUEIDENTIFIER NOT NULL, 
                    Quantity DECIMAL(18,4) NOT NULL,
                    Notes NVARCHAR(500) NULL,           
                    
                    CONSTRAINT PK_BomStructures PRIMARY KEY (ParentId, ChildId),
                    CONSTRAINT FK_BOM_Parent FOREIGN KEY (ParentId) REFERENCES Products(Id),
                    CONSTRAINT FK_BOM_Child FOREIGN KEY (ChildId) REFERENCES Products(Id),
                    CONSTRAINT CHK_NoSelfReference CHECK (ParentId <> ChildId)
                );
                
                
                CREATE TABLE ProductionSchedules (
                    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    ProductId UNIQUEIDENTIFIER NOT NULL,
                    Title NVARCHAR(200) NOT NULL,
                    Quantity DECIMAL(18,4) NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    EndDate DATETIME2 NOT NULL,
                    Status VARCHAR(50) NOT NULL,      
                    AssignedTo UNIQUEIDENTIFIER NULL, 
                    Granularity VARCHAR(20) NOT NULL, 
                    
                    CONSTRAINT FK_Schedule_Product FOREIGN KEY (ProductId) REFERENCES Products(Id),
                    CONSTRAINT FK_Schedule_User FOREIGN KEY (AssignedTo) REFERENCES Users(Id)
                );
                
                
                CREATE TABLE UploadedDocuments (
                    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    FileName NVARCHAR(255) NOT NULL,
                    FileSize BIGINT NOT NULL,         
                    MimeType VARCHAR(100) NOT NULL,   
                    UploadedBy UNIQUEIDENTIFIER NOT NULL,
                    Status VARCHAR(50) NOT NULL,      
                    ErrorMsg NVARCHAR(MAX) NULL,
                    UploadedAt DATETIME2 DEFAULT GETUTCDATE(),
                    
                    CONSTRAINT FK_Upload_User FOREIGN KEY (UploadedBy) REFERENCES Users(Id)
                );
                
                
                CREATE NONCLUSTERED INDEX IX_BomStructures_ParentId 
                ON BomStructures(ParentId) 
                INCLUDE (ChildId, Quantity, Notes);
                
                
                CREATE NONCLUSTERED INDEX IX_BomStructures_ChildId 
                ON BomStructures(ChildId) 
                INCLUDE (ParentId, Quantity, Notes);
                
                CREATE UNIQUE NONCLUSTERED INDEX IX_Products_SKU 
                ON Products(SKU);
                
                CREATE NONCLUSTERED INDEX IX_Products_Category 
                ON Products(Category);
                
                CREATE NONCLUSTERED INDEX IX_Schedules_Status_Date 
                ON ProductionSchedules(Status, StartDate)
                INCLUDE (ProductId, Quantity, AssignedTo);
                
                
                DECLARE @SillaId UNIQUEIDENTIFIER = NEWID();
                DECLARE @PataMaderaId UNIQUEIDENTIFIER = NEWID();
                DECLARE @RespaldoId UNIQUEIDENTIFIER = NEWID();
                DECLARE @ClavoId UNIQUEIDENTIFIER = NEWID();
                DECLARE @TablaMaderaId UNIQUEIDENTIFIER = NEWID();
                
                INSERT INTO Products (Id, SKU, Name, Category, Unit, Stock) VALUES
                (@SillaId, 'SLL-001', 'Silla de Comedor Premium', 'ProductoFinal', 'PZA', 50),
                (@PataMaderaId, 'PTA-M01', 'Pata de Madera Torneada', 'Subensamblaje', 'PZA', 200),
                (@RespaldoId, 'RSP-001', 'Respaldo Curvo', 'Subensamblaje', 'PZA', 80),
                (@ClavoId, 'CLV-2IN', 'Clavo de 2 Pulgadas', 'MateriaPrima', 'PZA', 5000),
                (@TablaMaderaId, 'TAB-PINO', 'Tabla de Pino 1x4', 'MateriaPrima', 'MTS', 300);
                
             
                INSERT INTO BomStructures (ParentId, ChildId, Quantity, Notes) VALUES
                (@SillaId, @PataMaderaId, 4, 'Pegar y clavar bien'),
                (@SillaId, @RespaldoId, 1, 'Alinear a 90 grados');
                
                INSERT INTO BomStructures (ParentId, ChildId, Quantity, Notes) VALUES
                (@PataMaderaId, @TablaMaderaId, 0.5, 'Corte en ángulo'),
                (@PataMaderaId, @ClavoId, 2, 'Clavado superior');
                
                INSERT INTO BomStructures (ParentId, ChildId, Quantity, Notes) VALUES
                (@RespaldoId, @TablaMaderaId, 1, 'Lijado fino requerido'),
                (@RespaldoId, @ClavoId, 4, 'Ocultar cabezas de clavo');

                INSERT INTO AppViews (ViewName, Description) VALUES
                ('/bom-tree', 'Vista del explosionado'),
                ('/schedule', 'Calendario de producción'),
                ('/upload', 'Subida de planos y documentos'),
                ('/users', 'Gestión de usuarios');
                
                DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
                DECLARE @SupId UNIQUEIDENTIFIER = NEWID();
                DECLARE @WorkerId UNIQUEIDENTIFIER = NEWID();
                
                INSERT INTO Users (Id, Name, Email, PasswordHash, Role) VALUES
                (@AdminId, 'El Jefe', 'admin@planta.com', '123456', 'admin'),
                (@SupId, 'Supervisor Juan', 'juan@planta.com', '123456', 'supervisor'),
                (@WorkerId, 'Operador Pedro', 'pedro@planta.com', '123456', 'worker');
                
                INSERT INTO RoleViews (Role, ViewId)
                SELECT 'admin', Id FROM AppViews WHERE ViewName != '/upload';
                
                INSERT INTO RoleViews (Role, ViewId)
                SELECT 'supervisor', Id FROM AppViews WHERE ViewName IN ('/bom-tree', '/schedule', '/upload');
                
                INSERT INTO RoleViews (Role, ViewId)
                SELECT 'worker', Id FROM AppViews WHERE ViewName IN ('/bom-tree', '/schedule');

            ";
                bomDb.Execute(initScript);
            }
        }
    }
}
