using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Microsoft.EntityFrameworkCore;
using System.Text;
using AdressverwaltungApi;
using AdressverwaltungApi.Controllers;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Options;
using AdressverwaltungApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Server-Kennung ("Server: Kestrel") nicht in Antworten preisgeben
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

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
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();

// === BENACHRICHTIGUNGSDIENST: E-Mail-Implementierung (B-03: SRP) ===
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

// === JWT-AUTHENTIFIZIERUNG ===
// Der Bearer-Token wird vom AuthController ausgestellt und bei jedem
// geschützten API-Aufruf im Authorization-Header mitgesendet.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    throw new InvalidOperationException("JWT-Schlüssel (Jwt:Key) ist nicht konfiguriert.");

// HS256 braucht mindestens 256 Bit; Platzhalter aus Vorlagen werden abgelehnt,
// weil sich mit einem bekannten Schlüssel beliebige Tokens fälschen lassen.
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < JwtOptions.MinKeyLength
    || jwtOptions.Key.StartsWith("dein-geheimer-jwt-schluessel"))
    throw new InvalidOperationException(
        "JWT-Schlüssel (Jwt:Key) ist zu kurz oder ein Platzhalter. Eigenen Schlüssel mit mindestens 32 Zeichen setzen, z.B. mit 'openssl rand -base64 32'.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// === ANMELDESCHUTZ: Sperre nach wiederholten Fehlversuchen pro E-Mail-Adresse ===
builder.Services.AddSingleton<ILoginThrottle, MemoryLoginThrottle>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            // Nur das Verfahren akzeptieren, mit dem JwtTokenService signiert
            ValidAlgorithms          = [SecurityAlgorithms.HmacSha256],
            ValidIssuer              = jwtOptions.Issuer,
            ValidAudience            = jwtOptions.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtOptions.Key)),
            // Claim "name" aus dem Token als Identity.Name verwenden
            // (wird für die Audit-Felder CreatedBy/ChangedBy benötigt)
            NameClaimType            = "name",
        };

        // Bei jeder Anfrage prüfen, ob es den Benutzer noch gibt und das Token
        // nicht widerrufen ist; dabei die aktuelle Rolle aus der Datenbank übernehmen.
        options.Events = new JwtBearerEvents { OnTokenValidated = TokenUserValidator.ValidateAsync };
    });

// Jeder Endpunkt verlangt ein gültiges JWT, auch ohne eigenes [Authorize] – das gilt
// damit ebenso für /odata und /odata/$metadata. Ausnahmen brauchen [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// === ODATA: EDM-Modell definieren ===
var modelBuilder = new ODataConventionModelBuilder();
modelBuilder.EnableLowerCamelCase(); // camelCase für alle Properties
modelBuilder.EntitySet<Adresse>("Adressen");
modelBuilder.EntitySet<City>("Cities");

// === CONTROLLERS + ODATA aktivieren ===
builder.Services.AddControllers(options => options.Filters.Add<ODataErrorDetailFilter>())
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
        .SetMaxTop(ODataCrudController<Adresse>.MaxPageSize)
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

// === DATENBANKSCHEMA UND SEED-DATEN BEIM START ===
using (var scope = app.Services.CreateScope())
{
    var db      = scope.ServiceProvider.GetRequiredService<AdresseDbContext>();
    var hasher  = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var config  = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // EnsureCreated erstellt alle Tabellen direkt aus dem EF-Modell.
    // Zuverlässiger als Migrate() in Docker-Umgebungen, da keine
    // Migrations-History-Konflikte entstehen können.
    db.Database.EnsureCreated();

    // Spalten nachziehen, die einer bestehenden Datenbank noch fehlen
    SchemaUpgrader.Apply(db);

    // B-01: Seed-Logik ausgelagert in DbSeeder (SRP)
    DbSeeder.Seed(db, hasher, config, app.Logger);
}

// === MIDDLEWARE PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Antworten enthalten Personendaten und Tokens: weder Browser noch Proxys sollen sie speichern
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next();
});

app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();   // ← muss VOR UseAuthorization stehen
app.UseAuthorization();
app.MapControllers();

app.Run();

// Macht die implizite Program-Klasse für WebApplicationFactory<Program>
// im Testprojekt (AdressverwaltungApi.Tests) sichtbar.
public partial class Program { }
