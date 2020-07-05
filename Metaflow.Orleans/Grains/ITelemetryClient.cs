using System;

namespace Metaflow.Orleans
{
    public interface ITelemetryClient
    {
        void TrackResult<TResource, TInput>(string id, Result<TResource> result);
        void TrackException<TResource, TInput>(MutationRequest request, string id, Exception ex);
        void TrackRequest<TResource, TInput>(MutationRequest request, string id);
    }
}
