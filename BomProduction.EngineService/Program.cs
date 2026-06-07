using BomProduction.EngineService.InitClases;
using BomProduction.EngineService.Repositories;
using BomProduction.EngineService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddScoped<BomRepository>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("http://localhost:4200") // El puerto default de Angular
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();


app.UseCors("AllowAngularApp");

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");



//-------------------------------------------------------------
// Area de endpoints
//-------------------------------------------------------------

app.MapGet("/api/bom/explosion/{rootId:guid}", async (Guid rootId, BomRepository repo) =>
{
    try
    {
        var explosion = await repo.GetBomExplosionAsync(rootId);

        if (explosion.Children.Count == 0 && explosion.Name == string.Empty)
            return Results.NotFound(new { Message = "Producto no encontrado o sin BOM" });

        return Results.Ok(new
        {
            Success = true,
            Message = "BOM Explosionado con éxito",
            Data = explosion
        });
    }
    catch (Exception ex)
    {
        // En producción logueamos el error y devolvemos un 500 genérico
        return Results.Problem($"Error explotando el BOM: {ex.Message}");
    }
});

//-------------------------------------------------------------
// Fin de area de endpoints
//-------------------------------------------------------------

//-------------------------------------------------------------
// Zona Initilizer
//-------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
DbInitializer.Initialize(connectionString!);

//-------------------------------------------------------------
// Fin de zona Initilizer
//-------------------------------------------------------------

app.Run();
