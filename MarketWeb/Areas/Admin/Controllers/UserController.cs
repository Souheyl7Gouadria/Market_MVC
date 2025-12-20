using Market.DataAccess.Data;
using Market.DataAccess.Repository;
using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MarketWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.Role_Admin)]
    public class UserController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public UserController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, IUnitOfWork unitOfwork)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfwork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            RoleManagementVM RoleVM = new RoleManagementVM()
            {
                //ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
                ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId, includeProperties:"Company"),
                RoleList = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                }),
                CompanyList = _unitOfWork.CompanyRepository.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };
            RoleVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();
            return View(RoleVM);
        }

        #region API CALLS
        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {
            string userId = roleManagementVM.ApplicationUser.Id;
            string oldRoleName = _userManager.GetRolesAsync(_unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser userFromDb = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId);

            if (roleManagementVM.ApplicationUser.Role != oldRoleName)
            {
                // user role has been updated
                
                if(roleManagementVM.ApplicationUser.Role == StaticDetails.Role_Company)
                {
                    userFromDb.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                }
                else if(oldRoleName == StaticDetails.Role_Company)
                {
                    // User was Company but is now changing to a different roleso remove company association
                    userFromDb.CompanyId = null;
                }
                _unitOfWork.ApplicationUserRepository.Update(userFromDb);
                _unitOfWork.Save();
                // here we can't just do :
                // userFromDb.Role = roleManagementVM.ApplicationUser.Role;
                // because 'Role' property is not a db column, [NotMapped], it was added for display purpose 
                _userManager.RemoveFromRoleAsync(userFromDb, oldRoleName).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if (oldRoleName == StaticDetails.Role_Company && userFromDb.CompanyId != roleManagementVM.ApplicationUser.CompanyId)
                {
                    userFromDb.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUserRepository.Update(userFromDb);
                    _unitOfWork.Save();
                }
            }

            return RedirectToAction("Index"); 
        }


        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = _unitOfWork.ApplicationUserRepository.GetAll(includeProperties: "Company").ToList();

            var userRoles = _unitOfWork.ApplicationUserRepository.GetAll(includeProperties: "Company").ToList();

            foreach (var user in userList)
            {
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new Company { Name = "" };
                }
            }
            return Json(new { data = userList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {

            var objFromDb = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == id);
            if(objFromDb == null)
            {
                return Json(new {success = false, message = "Error while Locking/Unlocking"});
            }
            if(objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // user is currently locked, we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1);
            }
            _unitOfWork.ApplicationUserRepository.Update(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Locking/Unlocking action is Successful" });
        }

        #endregion

    }
}
