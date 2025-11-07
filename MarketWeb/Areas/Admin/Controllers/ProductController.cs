using Market.DataAccess.Data;
using Market.DataAccess.Repository;
using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Product> Products = _unitOfWork.ProductRepository.GetAll().ToList();
            return View(Products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Product obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.ProductRepository.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
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
            Product ProductFromDb = _unitOfWork.ProductRepository.Get(u => u.Id == id);
            if (ProductFromDb == null)
            {
                return NotFound();
            }
            return View(ProductFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Product obj)
        {
            // Note: if the primary key name is not "Id", model binding will fail to bind the form data to the obj parameter, and will set the Id to 0
            // and will create a new object instead of updating the existing one. to avoid that we need to add a hidden input field in the form with the name of the primary key.
            if (ModelState.IsValid)
            {
                _unitOfWork.ProductRepository.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Product updated successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? ProductFromDb = _unitOfWork.ProductRepository.Get(u => u.Id == id);
            if (ProductFromDb == null)
            {
                return NotFound();
            }
            return View(ProductFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteProduct(int? id)
        {
            Product? ProductFromDb = _unitOfWork.ProductRepository.Get(u => u.Id == id);
            if (ProductFromDb == null)
            {
                return NotFound();
            }
            _unitOfWork.ProductRepository.Remove(ProductFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Product deleted  successfully";
            return RedirectToAction("Index");
        }
    }
}
