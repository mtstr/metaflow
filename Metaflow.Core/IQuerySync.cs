using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metaflow
{

    public interface IQuerySync<T>
    {
        Task UpdateQueryStore(string id, T entity);
    }
}