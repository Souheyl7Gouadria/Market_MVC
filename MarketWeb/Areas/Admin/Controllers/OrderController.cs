using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Mvc;

namespace MarketWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

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

            OrderVM orderVM = new()
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

            return View(orderVM);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> OrderHeaders = _unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser");

            // Normalize status to lowercase for comparison
            if (!string.IsNullOrEmpty(status))
            {
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
            }

            return Json(new { data = OrderHeaders });
        }
        #endregion

    }
}
