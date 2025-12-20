using Market.DataAccess.Data;
using Market.DataAccess.Repository;
using Market.DataAccess.Repository.IRepository;
using Market.Models;
using Market.Models.ViewModel;
using Market.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MarketWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;// used to access wwwroot folder
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> Products = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
            return View(Products);
        }

        public IActionResult CreateOrUpdate(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = GetCategoryList(),
                Product = new Product()
            };

            if (id == null || id == 0)
            {
                // create
                return View(productVM);
            }
            else
            {
                // update
                productVM.Product = _unitOfWork.ProductRepository.Get(u => u.Id == id, includeProperties: "ProductImages");
                if (productVM.Product == null)
                {
                    return NotFound();
                }
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult CreateOrUpdate(ProductVM productVM, List<IFormFile> files)
        {

            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.ProductRepository.Add(productVM.Product);
                    TempData["success"] = "Product created successfully";
                }
                else
                {
                    _unitOfWork.ProductRepository.Update(productVM.Product);
                    TempData["success"] = "Product updated successfully";
                }
                _unitOfWork.Save();

                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (files != null && files.Count > 0)
                {
                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        // create directory with images of that product
                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }
                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id
                        };

                        if (productVM.Product.ProductImages == null)
                        {
                            productVM.Product.ProductImages = new List<ProductImage>();
                        }
                        productVM.Product.ProductImages.Add(productImage);
                    }
                    _unitOfWork.ProductRepository.Update(productVM.Product);
                    _unitOfWork.Save();
                }

                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = GetCategoryList();

                return View(productVM);
            }
        }
        
        private IEnumerable<SelectListItem> GetCategoryList()
        {
            return _unitOfWork.CategoryRepository.GetAll()
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.CategoryId.ToString()
                });
        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToDelete = _unitOfWork.ProductImageRepository.Get(u => u.Id == imageId);
            int productId = imageToDelete.ProductId;
            if(imageToDelete != null)
            {
                if (!string.IsNullOrEmpty(imageToDelete.ImageUrl))
                {
                    // delete image from wwwroot
                   var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, imageToDelete.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOfWork.ProductImageRepository.Remove(imageToDelete);
                _unitOfWork.Save();

                TempData["success"] = "Image deleted successfully";
            }
            return RedirectToAction(nameof(CreateOrUpdate), new { id = productId });
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> Products = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = Products });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.ProductRepository.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new {success = false, message = "Error while deleting"});
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_hostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                // delete all images inside the folder
                string[] imagesPath = Directory.GetFiles(finalPath);
                foreach(string imagePath in imagesPath)
                {
                    System.IO.File.Delete(imagePath);
                }
                Directory.Delete(finalPath);
            }

            _unitOfWork.ProductRepository.Remove(productToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion

    }
}
