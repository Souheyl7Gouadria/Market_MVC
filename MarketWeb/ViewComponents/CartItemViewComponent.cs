using Market.DataAccess.Repository.IRepository;
using Market.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketWeb.ViewComponents
{
    // the backend file for the CartItem view component
    public class CartItemViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartItemViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims != null)
            {
                // session is null
                if (HttpContext.Session.GetInt32(StaticDetails.SessionCart) == null)
                {
                    HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.CartItemRepository.GetAll(u => u.ApplicationUserId == claims.Value).Count());
                    return View(HttpContext.Session.GetInt32(StaticDetails.SessionCart));
                }
                // we already have a session
                return View(HttpContext.Session.GetInt32(StaticDetails.SessionCart));
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
