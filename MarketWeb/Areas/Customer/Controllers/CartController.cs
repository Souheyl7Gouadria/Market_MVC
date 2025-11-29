using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
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

        [HttpPost]
        [ActionName("Summary")] // To differentiate from the GET Summary action
        // no need to pass cartItemVM because we are using [BindProperty]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // populate CartItemList again from Db because we might not have all fields in the form
            CartItemVM.CartItemList = _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

            CartItemVM.OrderHeader.OrderDate = System.DateTime.Now;
            CartItemVM.OrderHeader.ApplicationUserId = userId;

            // this is causing a bug (inserting a record with an existing id in AspNetUsers)
            // this is CRITICAL never populate a navigation property when adding a new record

            //CartItemVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId);
            ApplicationUser applicationUser = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId);

            foreach (var cart in CartItemVM.CartItemList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartItemVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            // To proceed with payement: use ApplicationUser to check if there is a company associated with the user
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // regular customer, we need to proceed to payment
                CartItemVM.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusPending;
                CartItemVM.OrderHeader.OrderStatus = StaticDetails.StatusPending;
            }
            else
            {
                // company user, we can delay the payment
                CartItemVM.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusDelayedPayment;
                CartItemVM.OrderHeader.OrderStatus = StaticDetails.StatusApproved;
            }

            _unitOfWork.OrderHeaderRepository.Add(CartItemVM.OrderHeader);
            _unitOfWork.Save();

            // create order details for each cart item
            foreach (var cart in CartItemVM.CartItemList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = CartItemVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetailRepository.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // stripe logic
            }

            return RedirectToAction(nameof(OrderConfirmation), new {id = CartItemVM.OrderHeader.Id});
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
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
