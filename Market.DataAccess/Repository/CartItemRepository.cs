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
    public class CartItemRepository : Repository<CartItem> , ICartItemRepository
    {

        private ApplicationDbContext _dbContext;
        public CartItemRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public void Update(CartItem cartItem)
        {
            _dbContext.CartItems.Update(cartItem);
        }
    }
}
