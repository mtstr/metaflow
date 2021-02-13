using System;

namespace Metaflow.Orleans
{
    public interface IEventSerializer
    {
        object Deserialize(Type type, string eventType, string json);
        string Name<TOwner, TResource, TInput>(object @event);
    }
}