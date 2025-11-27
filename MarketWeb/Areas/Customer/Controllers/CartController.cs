using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        
        private readonly IUnitOfWork _unitOfWork;
        public CartItemVM CartItemVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartItemVM = new ()
            {
                CartItemList = _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach(var cart in CartItemVM.CartItemList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartItemVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(CartItemVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            CartItemVM = new()
            {
                CartItemList = _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            CartItemVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId);
            CartItemVM.OrderHeader.Name = CartItemVM.OrderHeader.ApplicationUser.Name;
            CartItemVM.OrderHeader.PhoneNumber = CartItemVM.OrderHeader.ApplicationUser.PhoneNumber;
            CartItemVM.OrderHeader.StreetAddress = CartItemVM.OrderHeader.ApplicationUser.StreetAddress;
            CartItemVM.OrderHeader.City = CartItemVM.OrderHeader.ApplicationUser.City;
            CartItemVM.OrderHeader.State = CartItemVM.OrderHeader.ApplicationUser.State;
            CartItemVM.OrderHeader.PostalCode = CartItemVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in CartItemVM.CartItemList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartItemVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(CartItemVM);
        }

        public IActionResult Plus(int cardId)
        {
            var cartFromDb = _unitOfWork.CartItemRepository.Get(u => u.Id == cardId);
            cartFromDb.Count++;
            _unitOfWork.CartItemRepository.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cardId)
        {
            var cartFromDb = _unitOfWork.CartItemRepository.Get(u => u.Id == cardId);
            if (cartFromDb.Count <= 1)
            {
                _unitOfWork.CartItemRepository.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count--;
                _unitOfWork.CartItemRepository.Update(cartFromDb);
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cardId)
        {
            var cartFromDb = _unitOfWork.CartItemRepository.Get(u => u.Id == cardId);
            
            _unitOfWork.CartItemRepository.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        private double GetPriceBasedOnQuantity(CartItem cartItem)
        {
            if (cartItem.Count <= 50)
            {
                return cartItem.Product.Price;
            } else if (cartItem.Count <= 100)
            {
                return cartItem.Product.Price50;
            }
            else
            {
                return cartItem.Product.Price100;
            }
        }
    }
}
