namespace Metaflow
{
    public sealed class Nothing
    {
        private Nothing() { }
        public static Nothing Value() => new Nothing();
    }
}
