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
            .WithOrigins("http://localhost:3000") // Cambia a la URL de tu aplicaci�n React en producci�n
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Permitir credenciales para cookies
});

// Registra la clase AesEncryption con los valores obtenidos del archivo de configuraci�n
builder.Services.AddSingleton<AesEncryption>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new AesEncryption(configuration);
});

// Configuraci�n de la cadena de conexi�n MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)))); // Aseg�rate de usar la versi�n de MySQL adecuada

// Configurar Identity con Entity Framework Core
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configurar la autenticaci�n JWT
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
        ClockSkew = TimeSpan.Zero // Sin retraso para una expiraci�n precisa
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilitar CORS globalmente
app.UseCors("AllowReactApp");

// Crear las tablas autom�ticamente si no existen
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync(); // Ejecuta migraciones si no existen las tablas de manera as�ncrona
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Habilitar autenticaci�n
app.UseAuthorization();

app.MapControllers();

await app.RunAsync(); // Ejecutar la aplicaci�n de manera as�ncrona
