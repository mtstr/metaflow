using System.Threading.Tasks;

namespace Metaflow
{
    public interface IQueryStore<T>
    {
        Task Write(T entity, object queryableData);
    }
}