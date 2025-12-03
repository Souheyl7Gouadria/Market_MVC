using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        #endregion

    }
}
