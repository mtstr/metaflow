namespace Metaflow.Orleans
{
    public class CustomRequest<TResource, TInput>
    {
        public MutationRequest Request { get; }
        public TInput Input { get; }

        public CustomRequest(MutationRequest request, TInput input)
        {
            Request = request;
            Input = input;
        }
    }
}
