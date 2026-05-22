using System.Text;
using CyberCafe.Core.Interfaces;
using CyberCafe.Infrastructure.Data;
using CyberCafe.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Toggle between SQLite (local dev) and MySQL (production) via appsettings
var useMySQL = builder.Configuration.GetValue<bool>("UseMySQL");

if (useMySQL)
{
    var connStr = builder.Configuration.GetConnectionString("MySqlConnection");
    builder.Services.AddDbContext<CyberCafeDbContext>(options =>
        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));
}
else
{
    builder.Services.AddDbContext<CyberCafeDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")
                          ?? "Data Source=cybercafe.db"));
}

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key must be set in appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAccountService, AccountService>();

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "CyberCafe Booking API",
        Version = "v1",
        Description = "Manage computers, sessions, wallets, and authentication for a cyber cafe."
    });

    // Allow sending JWT in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter your JWT token in the text input below (no need to prefix with 'Bearer')."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-apply migrations on startup (convenient for dev/staging)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CyberCafeDbContext>();
    db.Database.Migrate();
}

// Swagger is always enabled — protect behind auth or restrict in production as needed
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CyberCafe Booking API v1");
    c.RoutePrefix = "swagger";
});

// HTTPS redirect disabled for local dev — re-enable when deploying with a valid cert
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
