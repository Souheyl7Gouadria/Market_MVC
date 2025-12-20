using Humanizer;
using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace MarketWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category,ProductImages");
            return View(productList);
        }

        public IActionResult Details(int id)
        {
            CartItem cart = new CartItem()
            {
                Product = _unitOfWork.ProductRepository.Get(u => u.Id == id, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = id
            };
            return View(cart);
        }

        [HttpPost]
        [Authorize] // only authenticated users can add to cart
        public IActionResult Details(CartItem cartItem)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cartItem.ApplicationUserId = userId;

            CartItem cartItemFromDb = _unitOfWork.CartItemRepository.Get(u => u.ApplicationUserId == userId && u.ProductId == cartItem.ProductId);

            if (cartItemFromDb != null)
            {
                // cartItem already exists in DB, update count
                cartItemFromDb.Count += cartItem.Count;
                _unitOfWork.CartItemRepository.Update(cartItemFromDb);
                _unitOfWork.Save();
            }
            else
            {
                // cartItem does not exist in DB, add new
                cartItem.Id = 0; // Reset Id to 0 to ensure EF treats this as a new entity
                _unitOfWork.CartItemRepository.Add(cartItem);
                _unitOfWork.Save();
                // add to session the count of cart items for the user
                HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == userId).Count());
            }
            TempData["success"] = "Item added to cart successfully!";
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
