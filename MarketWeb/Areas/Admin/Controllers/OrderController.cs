using Market.DataAccess.Repository.IRepository;
using Market.Models;
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
