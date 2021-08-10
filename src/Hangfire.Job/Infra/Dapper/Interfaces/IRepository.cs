using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Job.Infra.Dapper.Interfaces
{

    /// <summary>
    ///  Alerta!!!! Em Implementação
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<TEntity>: IDisposable
    {

        Task<IEnumerable<TEntity>> GetAllAsync();
        Task InsertAsync(TEntity t);
        Task UpdateAsync(TEntity t);
        Task<int> SaveRangeAsync(IEnumerable<TEntity> list);
        //Task DeleteRowAsync(Guid id);
        //Task<TEntity> GetAsync(Guid id);




    }
}
