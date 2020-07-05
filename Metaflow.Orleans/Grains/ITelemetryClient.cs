using System;

namespace Metaflow.Orleans
{
    public interface ITelemetryClient
    {
        void TrackResult<T>(Result<T> result);
        void TrackException(Exception ex);
        void TrackRequest<TInput>(MutationRequest request, TInput input);
    }
}
