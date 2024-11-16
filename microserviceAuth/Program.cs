using microserviceAuth.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using microserviceAuth.Encrypted;

var builder = WebApplication.CreateBuilder(args);

// Configurar el servicio CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy
            .WithOrigins("http://localhost:3000") // Cambia a la URL de tu aplicación React en producción
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Permitir credenciales para cookies
});

// Registra la clase AesEncryption con los valores obtenidos del archivo de configuración
builder.Services.AddSingleton<AesEncryption>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new AesEncryption(configuration);
});

// Configuración de la cadena de conexión MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)))); // Asegúrate de usar la versión de MySQL adecuada

// Configurar Identity con Entity Framework Core
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configurar la autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TuClaveSecretaAqui")), // Reemplaza con una clave secreta segura
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero // Sin retraso para una expiración precisa
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilitar CORS globalmente
app.UseCors("AllowReactApp");

// Crear las tablas automáticamente si no existen
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync(); // Ejecuta migraciones si no existen las tablas de manera asíncrona
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Habilitar autenticación
app.UseAuthorization();

app.MapControllers();

await app.RunAsync(); // Ejecutar la aplicación de manera asíncrona
