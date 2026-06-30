namespace EvStorionX.Api.Services;

/// <summary>
/// Per-scope holder that lets the API inject a specific RunId into
/// <see cref="EvStorionX.Application.Pipeline.OrchestratorOptions"/> via
/// a Scoped <c>IPostConfigureOptions</c> registration.
/// </summary>
internal sealed class ActiveRunContext
{
    /// <summary>RunId to use for the current migration scope; <see cref="Guid.Empty"/> means "use default".</summary>
    public Guid RunId { get; set; }
}
