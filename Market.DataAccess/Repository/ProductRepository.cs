using Market.DataAccess.Data;
using Market.DataAccess.Repository.IRepository;
using Market.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Market.DataAccess.Repository
{
    public class ProductRepository : Repository<Product> , IProductRepository
    {

        private ApplicationDbContext _dbContext;
        public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public void Update(Product product)
        {
            _dbContext.Products.Update(product);
        }
    }
}
