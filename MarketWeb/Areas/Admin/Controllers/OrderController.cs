using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace MarketWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {

            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeaderRepository.Get(
                    u => u.Id == id,
                    includeProperties: "ApplicationUser"
                ),
                OrderDetailList = _unitOfWork.OrderDetailRepository.GetAll(
                    filter: u => u.OrderHeaderId == id,
                    includeProperties: "Product"
                )
            };

            return View(OrderVM);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == orderHeaderId);

            if (orderHeader.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
            {
                // order is made by company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, StaticDetails.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            return View(orderHeaderId);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> OrderHeaders;

            if (User.IsInRole(StaticDetails.Role_Admin) || User.IsInRole(StaticDetails.Role_Employee))
            {
                OrderHeaders = _unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                OrderHeaders = _unitOfWork.OrderHeaderRepository.GetAll(
                    u => u.ApplicationUserId == userId,
                    includeProperties: "ApplicationUser"
                ).ToList();
            }

            switch (status.ToLower())
            {
                case "pending":
                    OrderHeaders = OrderHeaders.Where(u => u.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    OrderHeaders = OrderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusInProcess);
                    break;
                case "completed":
                    OrderHeaders = OrderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusShipped);
                    break;
                case "approved":
                    OrderHeaders = OrderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusApproved);
                    break;
                default:
                    // "all" - return all orders
                    break;
            }

            return Json(new { data = OrderHeaders });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderVM.OrderHeader.Id);

            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            
            _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order details updated successfully.";

            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeaderRepository.UpdateStatus(OrderVM.OrderHeader.Id, StaticDetails.StatusInProcess);
            _unitOfWork.Save();

            TempData["success"] = "Order status updated to 'In Process'.";

            return RedirectToAction(nameof(Details), new {id = OrderVM.OrderHeader.Id});
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult ShipOrder()
        {
            // update carrier and tracking number
            var orderHeaderFromDb = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = StaticDetails.StatusShipped;
            orderHeaderFromDb.ShoppingDate = DateTime.Now;
            if (orderHeaderFromDb.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order shipped successfully.";

            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeadeFromDb = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeadeFromDb.PaymentStatus == StaticDetails.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeadeFromDb.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeadeFromDb.Id, StaticDetails.StatusCancelled, StaticDetails.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeadeFromDb.Id, StaticDetails.StatusCancelled, StaticDetails.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["success"] = "Order cancelled successfully.";

            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        public IActionResult Pay()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetailList = _unitOfWork.OrderDetailRepository.GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            // stripe logic, following official documentation

            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/Details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };


            foreach (var item in OrderVM.OrderDetailList)
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
            _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        #endregion

    }
}
