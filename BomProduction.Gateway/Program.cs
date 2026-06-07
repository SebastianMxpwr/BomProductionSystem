using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. EXTRAER LA LLAVE
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? throw new ArgumentNullException("Falta el SecretKey");

// 2. CONFIGURAR LA VALIDACIÓN DEL TOKEN (Igualito que en el QueryService)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// 3. CREAR LA POLÍTICA "DEFAULT" QUE PUSIMOS EN EL JSON
builder.Services.AddAuthorization(options =>
{
    // "default" exige que cualquier wey que pase tenga un token válido
    options.AddPolicy("RequireJwt", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAngular");

// 4. EL ORDEN IMPORTA MUCHO AQUÍ
app.UseAuthentication(); // "Pásame tu ID"
app.UseAuthorization();  // "¿Tienes permiso para esta zona?"

app.MapReverseProxy();   // "Pásale, güero"

app.Run();