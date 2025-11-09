using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Market.DataAccess.Data;
using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Microsoft.EntityFrameworkCore;


namespace Market.DataAccess.Repository
{
    // Provides common CRUD operations for any entity type T
    public class Repository<T> : IRepository<T> where T : class
    {

        private readonly ApplicationDbContext _dbContext;
        internal DbSet<T> _dbSet;// generic DbSet for entity type T
        public Repository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();// dynamically gets the DbSet for the given entity type T
        }
        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public T Get(Expression<Func<T, bool>> filter, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;
            query = query.Where(filter);
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries
                    ))
                {
                    query = query.Include(includeProp);
                }
            }
            return query.FirstOrDefault();
        }

        public IEnumerable<T> GetAll(string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp); // specifies related entities (navigation property) to include in query results
                }
            }
            return query.ToList();
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entity)
        {
            _dbSet.RemoveRange(entity);
        }
    }
}
