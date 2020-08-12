using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;

namespace Metaflow.Orleans
{
    public class AppInsightsTelemetryClient : ITelemetryClient
    {
        private readonly TelemetryClient _telemetry;
        private readonly string CorrelationId = Guid.NewGuid().ToString();

        public AppInsightsTelemetryClient(TelemetryClient telemetry)
        {
            _telemetry = telemetry;
        }

        public void TrackEvents<TResource, TInput>(string id, IEnumerable<object> events)
        {
            foreach (var @event in events)
            {

                Dictionary<string, string> telemetry = new Dictionary<string, string>
                {
                    ["resource"] = typeof(TResource).Name,
                    ["input"] = typeof(TInput).Name,
                    ["resource_id"] = id,
                    ["correlation_id"] = CorrelationId
                };

                if (@event is Rejected<TResource, TInput> reject)
                {
                    telemetry["success"] = "false";
                    telemetry["reason"] = reject.Reason;
                }
                else
                {
                    telemetry["success"] = "true";
                }

                _telemetry.TrackEvent(@event.Name<TResource,TInput>(), telemetry);
            }
        }

        public void TrackException<TResource, TInput>(MutationRequest request, string id, Exception ex)
        {
            _telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["resource"] = typeof(TResource).Name,
                ["input"] = typeof(TInput).Name,
                ["resource_id"] = id,
                ["correlation_id"] = CorrelationId
            });
        }

        public void TrackRequest<TResource, TInput>(MutationRequest request, string id)
        {
            _telemetry.TrackEvent(request.ToString(), new Dictionary<string, string>
            {
                ["resource"] = typeof(TResource).Name,
                ["input"] = typeof(TInput).Name,
                ["resource_id"] = id,
                ["correlation_id"] = CorrelationId
            });
        }
    }
}
