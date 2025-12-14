using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
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
        [Authorize]
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

            // if regular customer, proceed to payment
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // stripe logic, following official documentation

                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={CartItemVM.OrderHeader.Id}",
                    CancelUrl = domain + "Customer/Cart/Index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };


                foreach (var item in CartItemVM.CartItemList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // 19.50 => 1950
                            Currency = "usd", // Tunisia is not supported by stripe
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);
                // paymentIntentId is null for the moment, it only gets populated after the payment is successful
                _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(CartItemVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            return RedirectToAction(nameof(OrderConfirmation), new {id = CartItemVM.OrderHeader.Id});
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader.PaymentStatus != StaticDetails.PaymentStatusDelayedPayment)
            {
                // order is made by customer
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeaderRepository.UpdateStatus(id, StaticDetails.StatusApproved, StaticDetails.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            // clear the shopping cart
            List<CartItem> cartItems = _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.CartItemRepository.RemoveRange(cartItems);
            _unitOfWork.Save();

            // reset session cart count
            HttpContext.Session.SetInt32(StaticDetails.SessionCart, 0);

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
                HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count()-1);
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
            HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
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
