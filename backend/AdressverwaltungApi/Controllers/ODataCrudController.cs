using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// Generische Basisklasse für OData-CRUD-Controller.
/// Gemeinsame CRUD-Logik (GET/POST/PATCH/DELETE) wird hier implementiert,
/// um Duplikation zwischen AdressenController und CitiesController zu vermeiden (DRY).
///
/// Unterklassen definieren:
///   - Entities          → welches DbSet verwendet wird
///   - EntityDisplayName → für benutzerfreundliche Fehlermeldungen
///   - OnCreatedAsync    → optional: Folgeaktion nach dem Erstellen
/// </summary>
[Authorize]   // Alle abgeleiteten Controller (Adressen, Cities) erfordern gültigen JWT
public abstract class ODataCrudController<TEntity> : ODataController
    where TEntity : class
{
    protected readonly AdresseDbContext _context;

    /// <summary>Das DbSet, auf dem dieser Controller operiert.</summary>
    protected abstract DbSet<TEntity> Entities { get; }

    /// <summary>Anzeigename der Entität für Fehlermeldungen (z.B. "Adresse", "Stadt").</summary>
    protected abstract string EntityDisplayName { get; }

    protected ODataCrudController(AdresseDbContext context)
    {
        _context = context;
    }

    // GET /odata/{EntitySet}
    [EnableQuery]
    public virtual IActionResult Get()
        => Ok(Entities.AsNoTracking());

    // GET /odata/{EntitySet}(5)
    [EnableQuery]
    public virtual async Task<IActionResult> Get([FromRoute] int key)
    {
        var entity = await Entities.FindAsync(key);

        if (entity is null)
            return EntityNotFound(key);

        return Ok(entity);
    }

    // POST /odata/{EntitySet}
    public virtual async Task<IActionResult> Post([FromBody] TEntity entity)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Den Schlüssel vergibt die Datenbank. Ein mitgeschickter Wert wird verworfen,
        // sonst liessen sich Ids vorbelegen, die später mit der Sequenz kollidieren.
        var entry = _context.Entry(entity);
        foreach (var name in KeyPropertyNames)
            entry.Property(name).CurrentValue = entry.Property(name).Metadata.Sentinel;

        Entities.Add(entity);
        await _context.SaveChangesAsync();
        await OnCreatedAsync(entity);

        return Created(entity);
    }

    // PATCH /odata/{EntitySet}(5)
    public virtual async Task<IActionResult> Patch(
        [FromRoute] int key,
        [FromBody] Delta<TEntity> delta)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = await Entities.FindAsync(key);

        if (entity is null)
            return EntityNotFound(key);

        // Der Schlüssel ist nicht änderbar (EF Core würde sonst mit einer Ausnahme abbrechen)
        foreach (var name in KeyPropertyNames)
            delta.UpdatableProperties.Remove(name);

        delta.Patch(entity);

        if (!TryValidateModel(entity))
            return BadRequest(ModelState);

        await _context.SaveChangesAsync();

        return Updated(entity);
    }

    // DELETE /odata/{EntitySet}(5)
    public virtual async Task<IActionResult> Delete([FromRoute] int key)
    {
        var entity = await Entities.FindAsync(key);

        if (entity is null)
            return EntityNotFound(key);

        Entities.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Erweiterungspunkt: wird nach dem Speichern einer neuen Entität aufgerufen (B-11).
    /// Unterklassen hängen hier Folgeaktionen an, statt Post zu kopieren.
    /// </summary>
    protected virtual Task OnCreatedAsync(TEntity entity) => Task.CompletedTask;

    private IEnumerable<string> KeyPropertyNames
        => _context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties.Select(p => p.Name);

    private IActionResult EntityNotFound(int key)
        => NotFound($"{EntityDisplayName} mit Id {key} wurde nicht gefunden.");
}
