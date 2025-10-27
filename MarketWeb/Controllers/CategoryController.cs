using MarketWeb.Data;
using MarketWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public CategoryController(ApplicationDbContext applicationDbContext )
        {
             _dbContext = applicationDbContext;
        }
        public async Task<IActionResult> Index()
        {
            List<Category> Categories = await _dbContext.Categories.ToListAsync();
            return View(Categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category obj)
        {
            if (ModelState.IsValid)
            {
                await _dbContext.Categories.AddAsync(obj);
                await _dbContext.SaveChangesAsync();
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null || id == 0)
            {
                return NotFound();
            }
            Category categoryFromDb = _dbContext.Categories.Find(id);
            if(categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category obj)
        {
            // Note: if the primary key name is not "Id", model binding will fail to bind the form data to the obj parameter, and will set the Id to 0
            // and will create a new object instead of updating the existing one. to avoid that we need to add a hidden input field in the form with the name of the primary key.
            if (ModelState.IsValid)
            {
                _dbContext.Categories.Update(obj);
                await _dbContext.SaveChangesAsync();
                TempData["success"] = "Category updated successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? categoryFromDb = _dbContext.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            Category? categoryFromDb = _dbContext.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            _dbContext.Categories.Remove(categoryFromDb);
            await _dbContext.SaveChangesAsync();
            TempData["success"] = "Category deleted  successfully";
            return RedirectToAction("Index");
        }
    }
}
