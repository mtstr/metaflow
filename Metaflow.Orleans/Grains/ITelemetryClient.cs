using System;
using System.Collections.Generic;
using Metaflow;
namespace Metaflow.Orleans
{
    public interface ITelemetryClient
    {
        void TrackEvents<TResource, TInput>(string id, IEnumerable<object> events);
        void TrackException<TResource, TInput>(Operation operation, string id, Exception ex);
        void TrackRequest<TResource, TInput>(Operation operation, string id);
    }
}
