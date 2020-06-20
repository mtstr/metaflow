namespace Metaflow.Orleans
{
    public class GrainState<T>
    {
        public T Value { get; internal set; }
        public bool Exists { get; internal set; }
    }
}
