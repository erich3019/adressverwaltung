using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Microsoft.EntityFrameworkCore;
using System.Text;
using AdressverwaltungApi;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;

var builder = WebApplication.CreateBuilder(args);

// === DATENBANK: Entity Framework Core mit PostgreSQL ===
builder.Services.AddDbContext<AdresseDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// === HTTP-KONTEXT: Wird für Audit-Felder (CreatedBy/ChangedBy) benötigt ===
builder.Services.AddHttpContextAccessor();

// === PASSWORT-HASHING: ASP.NET Core PasswordHasher (PBKDF2 + Salt) ===
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// === E-MAIL-DIENST: SMTP-Implementierung (austauschbar via Interface) ===
builder.Services.AddScoped<IEmailService, EmailService>();

// === BENACHRICHTIGUNGSDIENST: E-Mail-Implementierung (B-03: SRP) ===
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

// === JWT-AUTHENTIFIZIERUNG ===
// Der Bearer-Token wird vom AuthController ausgestellt und bei jedem
// geschützten API-Aufruf im Authorization-Header mitgesendet.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT-Schlüssel (Jwt:Key) ist nicht konfiguriert.");

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
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

// === ODATA: EDM-Modell definieren ===
var modelBuilder = new ODataConventionModelBuilder();
modelBuilder.EnableLowerCamelCase(); // camelCase für alle Properties
modelBuilder.EntitySet<Adresse>("Adressen");
modelBuilder.EntitySet<City>("Cities");

// === CONTROLLERS + ODATA aktivieren ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // camelCase für REST-Antworten (auth, settings)
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .SetMaxTop(100)
        .Count()
        .Expand()
        .AddRouteComponents("odata", modelBuilder.GetEdmModel())
    );

// === CORS: Nur Frontend-Ursprung erlauben ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://frontend:3000"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger nur in Development (nicht in Production!)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// === DATENBANK MIGRATION BEIM START ===
using (var scope = app.Services.CreateScope())
{
    var db      = scope.ServiceProvider.GetRequiredService<AdresseDbContext>();
    var hasher  = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var config  = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // EnsureCreated erstellt alle Tabellen direkt aus dem EF-Modell.
    // Zuverlässiger als Migrate() in Docker-Umgebungen, da keine
    // Migrations-History-Konflikte entstehen können.
    db.Database.EnsureCreated();

    // B-01: Seed-Logik ausgelagert in DbSeeder (SRP)
    DbSeeder.Seed(db, hasher, config);
}

// === MIDDLEWARE PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();   // ← muss VOR UseAuthorization stehen
app.UseAuthorization();
app.MapControllers();

app.Run();
