using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.OData;

namespace AdressverwaltungApi;

/// <summary>
/// Entfernt "innererror" aus OData-Fehlerantworten. OData legt dort Ausnahmetyp und
/// Stacktrace ab (z.B. bei einer ungültigen $top-Angabe) – das gehört nicht zum Client.
/// Die eigentliche Fehlermeldung bleibt erhalten.
/// </summary>
public class ODataErrorDetailFilter : IAlwaysRunResultFilter
{
    // Einträge mit Angaben zur Ausnahme (Schlüssel aus SerializableErrorKeys von OData)
    private static readonly string[] ExceptionKeys =
        ["ExceptionMessage", "type", "stacktrace", "InnerException"];

    public void OnResultExecuting(ResultExecutingContext context)
    {
        switch (context.Result)
        {
            // [EnableQuery] meldet ungültige Abfragen als SerializableError
            case ObjectResult { Value: SerializableError error }:
                foreach (var key in ExceptionKeys)
                    error.Remove(key);
                break;

            case ObjectResult { Value: ODataError error }:
                error.InnerError = null;
                break;

            case IODataErrorResult { Error: not null } result:
                result.Error.InnerError = null;
                break;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
