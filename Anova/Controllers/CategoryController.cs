using Microsoft.AspNetCore.Mvc;
using Anova_DataAccess;
using Anova_Models;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Anova_Utility;
using Anova_DataAccess.Repository.IRepository;

namespace Anova.Controllers
{
    [Authorize(Roles = WC.AdminRole)]

    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _catRepo;

        public CategoryController(ICategoryRepository catRepo)
        {
            _catRepo = catRepo;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objList = _catRepo.GetAll();
            return View(objList);
        }

        // GET - Create
        public IActionResult Create()
        {

            return View();
        }

        // POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if (ModelState.IsValid)
            {
                _catRepo.Add(obj);
                _catRepo.Save();
                TempData[WC.Success] = "Category created successfully";
                return RedirectToAction("Index");
            }
            TempData[WC.Error] = "Error while creating category";
            return View(obj);
        }

        // GET - Edit
        public IActionResult Edit(int? Id)
        {
            if (Id == null || Id == 0)
            {
                return NotFound();
            }
            var obj = _catRepo.Find(Id.GetValueOrDefault());
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (ModelState.IsValid)
            {
                _catRepo.Update(obj);
                _catRepo.Save();
                TempData[WC.Success] = "Action completed successfully";
                return RedirectToAction("Index");
            }
            TempData[WC.Error] = "Error";
            return View(obj);
        }


        // GET - Dalete
        public IActionResult Delete(int? Id)
        {
            if (Id == null || Id == 0)
            {
                return NotFound();
            }
            var obj = _catRepo.Find(Id.GetValueOrDefault());
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }

        // POST - Dalete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? Id)
        {
            var obj = _catRepo.Find(Id.GetValueOrDefault());
            if (obj == null)
            {
                TempData[WC.Error] = "Error";
                return NotFound();
            }
            _catRepo.Remove(obj);
            _catRepo.Save();
            TempData[WC.Success] = "Category deleted successfully";
            return RedirectToAction("Index");
        }
    }
}
