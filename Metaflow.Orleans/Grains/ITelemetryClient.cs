using System;
using System.Collections.Generic;

namespace Metaflow.Orleans
{
    public interface ITelemetryClient
    {
        void TrackException<TResource, TInput>(MutationRequest request, string id, Exception ex);
        void TrackRequest<TResource, TInput>(MutationRequest request, string id);
    }
}
