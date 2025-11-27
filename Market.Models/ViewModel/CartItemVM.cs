using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Market.Models.ViewModel
{
    public class CartItemVM
    {
        public IEnumerable<CartItem> CartItemList { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
