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
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            string RoleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userId)?.RoleId;

            RoleManagementVM RoleVM = new RoleManagementVM()
            {
                ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
                RoleList = _db.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                }),
                CompanyList = _db.Companies.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };
            RoleVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == RoleId)?.Name;
            return View(RoleVM);
        }

        #region API CALLS
        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {
            // get the previous role id from db
            string oldRoleId = _db.UserRoles.FirstOrDefault(u => u.UserId == roleManagementVM.ApplicationUser.Id).RoleId;
            string oldRoleName = _db.Roles.FirstOrDefault(u => u.Id == oldRoleId).Name;

            if(roleManagementVM.ApplicationUser.Role != oldRoleName)
            {
                // user role has been updated
                ApplicationUser userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagementVM.ApplicationUser.Id);
                if(roleManagementVM.ApplicationUser.Role == StaticDetails.Role_Company)
                {
                    userFromDb.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                }
                else if(oldRoleName == StaticDetails.Role_Company)
                {
                    // User was Company but is now changing to a different roleso remove company association
                    userFromDb.CompanyId = null;
                }
                _db.SaveChanges();
                // here we cant just do :
                // userFromDb.Role = roleManagementVM.ApplicationUser.Role;
                // because 'Role' property is not a db column, [NotMapped], it was added for display purpose 
                _userManager.RemoveFromRoleAsync(userFromDb, oldRoleName).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }

            return RedirectToAction("Index"); 
        }


        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = _db.ApplicationUsers.Include(u => u.Company).ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in userList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id)?.RoleId;
                user.Role = roles.FirstOrDefault(r => r.Id == roleId).Name;

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

            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
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
            _db.SaveChanges();
            return Json(new { success = true, message = "Locking/Unlocking action is Successful" });
        }

        #endregion

    }
}
