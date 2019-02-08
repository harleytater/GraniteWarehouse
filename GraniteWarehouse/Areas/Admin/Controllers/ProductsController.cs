using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraniteWarehouse.Data;
using GraniteWarehouse.Models.ViewModels;
using GraniteWarehouse.Utility;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GraniteWarehouse.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly HostingEnvironment _hotsingEnviroment;
        [BindProperty]
        public ProductViewModel ProductsVM { get; set; }
        public ProductsController(ApplicationDbContext db, HostingEnvironment hotsingEnviroment)
        {
            _db = db;
            _hotsingEnviroment = hotsingEnviroment;
            ProductsVM = new ProductViewModel()
            {
                ProductTypes = _db.ProductTypes.ToList(),
                SpecialTags = _db.SpecialTags.ToList(),
                Products = new Models.Products()
            };
        }
        public async Task<IActionResult> Index()
        {
            var products = _db.Products.Include(mbox => mbox.ProductTypes).Include(m => m.SpecialTags);
            return View(await products.ToListAsync());
        }

        //Get: product Create

        public IActionResult Create()
        {
            return View(ProductsVM);//because I want dropdowns
        }

        //Post: product create
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()//we have bindpropterty so no parameters are necessary
        {
            if (!ModelState.IsValid)
            {
                return View(ProductsVM);
            }

            //_db.Products.Add(ProductsVM.Products);
            //await _db.SaveChangesAsync();

            //Product was saved, but the physical image...

            //same the physical image

            string webRootPath = _hotsingEnviroment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var productsFromDb = _db.Products.Find(ProductsVM.Products.Id);

            if (files.Count != 0)
            {
                //Image(s) has been uploaded with form

                var uploads = Path.Combine(webRootPath, SD.ImageFolder);
                var extension = Path.GetExtension(files[0].FileName);

                using (var filestream = new FileStream(Path.Combine(uploads, ProductsVM.Products.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(filestream); //moves to sever and renames
                }

                //now I know the new image name, so I can same the STRING image to the database

                productsFromDb.Image = @"\" + SD.ImageFolder + @"\" + ProductsVM.Products.Id + extension;                
            }
            else
            {
                //user didnt give an image so we'll upload the placehouder
                var uploads = Path.Combine(webRootPath, SD.ImageFolder+@"\"+SD.DefaultProductImage);
                System.IO.File.Copy(uploads, webRootPath + @"\" +SD.ImageFolder+@"\"+ ProductsVM.Products.Id + ".jpg");
                productsFromDb.Image = @"\" + SD.ImageFolder + @"\" + ProductsVM.Products.Id + ".jpg";
            }
            
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //Get Edit

        public async Task<IActionResult> Edit (int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ProductsVM.Products = await _db.Products.Include(m => m.SpecialTags)
                                                    .Include(m => m.SpecialTags)
                                                    .Include(m => m.ProductTypes)
                                                    .SingleOrDefaultAsync(m => m.Id == id);
            if (ProductsVM.Products == null)
            {
                return NotFound();
            }

            return View(ProductsVM);
        }
    }
}