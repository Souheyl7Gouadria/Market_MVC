using Market.DataAccess.Data;
using Market.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Market.DataAccess.Repository
{
    // Provide a single point of access to all repositories
    // Manages database transactions (atomicy), provides a single save() method to all repositories
    // reduces number of database calls by batching changes together

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        public ICategoryRepository CategoryRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            CategoryRepository = new CategoryRepository(_dbContext);
            ProductRepository = new ProductRepository(_dbContext);
        }

        // Single save method, commits all tracked changes
        public void Save()
        {
            _dbContext.SaveChanges();
        }
    }
}
