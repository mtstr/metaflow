namespace Metaflow.Orleans
{
    public class GrainState<T>
    {
        public T Value { get; set; }
        public bool Exists { get; set; }
    }
}
