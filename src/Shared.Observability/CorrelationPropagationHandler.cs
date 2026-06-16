using Shared.Messaging;

namespace Shared.Observability;

/// <summary>
/// An HttpClient message handler that copies the ambient correlation id onto
/// outgoing requests as X-Correlation-ID. This is how synchronous calls — such as
/// OrderService validating a product, or the BFF aggregating order + product data —
/// keep the same correlation id as the request that triggered them.
/// </summary>
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
