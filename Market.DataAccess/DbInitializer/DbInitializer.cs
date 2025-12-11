using Market.DataAccess.Data;
using Market.Models;
using Market.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Market.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public void Initialize()
        {
            // apply migrations if they are not applied
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception) { }

            // crete roles
            if (!_roleManager.RoleExistsAsync(StaticDetails.Role_Costumer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Costumer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Company)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Employee)).GetAwaiter().GetResult();

                // create admin user
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@market.com",
                    Email = "admin@market.com",
                    Name = "Souheyl Gouadria",
                    PhoneNumber = "1234567890",
                    StreetAddress = "Rue Suéde, Sousse",
                    City = "Sousse",
                    State = "State",
                    PostalCode = "12345"
                }, "Admin123!").GetAwaiter().GetResult();

                // Assign admin role
                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@market.com");
                _userManager.AddToRoleAsync(user, StaticDetails.Role_Admin).GetAwaiter().GetResult();
            }

            return;
        }
    }
}
