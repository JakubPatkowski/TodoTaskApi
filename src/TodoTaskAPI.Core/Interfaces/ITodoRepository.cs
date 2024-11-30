using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Core.Entities;

namespace TodoTaskAPI.Core.Interfaces
{
    public interface ITodoRepository
    {
        // Return Touple of Enumerated List of Todos and List size
        Task<(IEnumerable<Todo> Items, int TotalCount)> GetAllAsync(int  pageIndex, int pageSize);
    }
}