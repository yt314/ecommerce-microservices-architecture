using Shared.Messaging;

namespace Shared.Observability;

// Copies the ambient correlation id onto outgoing HttpClient calls (e.g.
// OrderService -> ProductCatalogService) so they carry the same id as the
// request that triggered them.
public class CorrelationPropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationContext.Current;
        if (!string.IsNullOrWhiteSpace(correlationId) &&
            !request.Headers.Contains(CorrelationConstants.HeaderName))
        {
            request.Headers.Add(CorrelationConstants.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
