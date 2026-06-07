using BomProduction.QueryService;
using BomProduction.Shared.Models;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});

// 1. Configurar JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? throw new ArgumentNullException("JWT Secret missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false, // En producción puedes validar el dominio
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 2. Configurar CORS (Para que Angular le pueda pegar directo si fuera necesario)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAngularApp");

// ACTIVAMOS LA SEGURIDAD EN EL PIPELINE
app.UseAuthentication();
app.UseAuthorization();


//-------------------------------------------------------------
// Area de endpoints
//-------------------------------------------------------------

app.MapPost("/api/auth/login", async (LoginRequest request, IConfiguration config) =>
{
    using var db = new SqlConnection(config.GetConnectionString("DefaultConnection"));

    // 1. Validamos al usuario
    var user = await db.QuerySingleOrDefaultAsync<User>(
        "SELECT Id, Name, Email, Role FROM Users WHERE Email = @Email AND PasswordHash = @Password",
        new { request.Email, request.Password });

    if (user == null) return Results.Unauthorized();

    // 2. Traemos las vistas dinámicas a las que tiene derecho
    var views = await db.QueryAsync<string>(
        @"SELECT a.ViewName 
          FROM RoleViews r 
          INNER JOIN AppViews a ON r.ViewId = a.Id 
          WHERE r.Role = @Role",
        // CAMBIO AQUÍ: Convertimos el Enum a texto en minúsculas ('admin', 'supervisor', etc.)
        new { Role = user.Role.ToString().ToLower() });

    // 3. Generamos el Token JWT (Firma digital)
    var key = Encoding.ASCII.GetBytes(config["JwtSettings:SecretKey"]!);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        }),
        Expires = DateTime.UtcNow.AddHours(8),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);

    // 4. Se lo mandamos a Angular
    return Results.Ok(new
    {
        Success = true,
        Token = tokenHandler.WriteToken(token),
        User = user,
        AllowedViews = views // <-- ¡Oro puro para tu Guard de Angular!
    });
});


//-------------------------------------------------------------
// Fin de area de endpoints
//-------------------------------------------------------------

app.Run();


