﻿
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using ShoraWebsite.Data;
using ShoraWebsite.Models;
using System.Diagnostics;

namespace ShoraWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userM;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userM)
        {
            _logger = logger;
            _context = context;
            _userM = userM;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                Console.WriteLine("Admin");
            }
            return View();
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


        
        //public async Task<IActionResult> EditarEmail()
        //{

        //    if (User != null)
        //    {

        //        var user = await _userM.FindByNameAsync(User.Identity.Name);
        //        if (User.Identity.Name != user.UserName)
        //        {
        //            return NotFound();
        //        }

        //        ViewData["OldEmail"] = user.Email;

        //        ViewData["Id"] = user.Id;

        //        return View();
        //    }
        //    else
        //    {
        //        return Error();
        //    }

        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditarEmail(EmailChangerModel changerModel)
        //{

        //    var user = await _userM.FindByIdAsync(changerModel.UserId);

        //    if (changerModel.NewEmail == string.Empty)
        //    {
        //        return View();
        //    }

           
        //    if (ModelState.IsValid)
        //    {
        //        //update email

                
               

        //        user.Email = changerModel.NewEmail;

        //        user.NormalizedEmail = changerModel.NewEmail.Normalize();

        //        await _userM.UpdateAsync(user);
        //        return RedirectToAction(nameof(Index));
        //    }
        //    else
        //    {
        //        return Error();
        //    }

        //}
    }
}