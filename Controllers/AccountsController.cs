using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ShoraWebsite.Models;
using ShoraWebsite.Data.Migrations;

namespace ShoraWebsite.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AccountsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [Authorize(Roles = "Admin")]
        public IActionResult AdminPainel()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {

            var users = _userManager.Users;
            var model = new List<UsersRolesViewModel>();
            foreach (var user in users)
            {
                var roles = _userManager.GetRolesAsync(user).Result;
                model.Add(new UsersRolesViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = roles.SingleOrDefault()
                });
            }
            return View(model);
        }


        [Authorize(Roles = "Admin,Cliente")]
        public IActionResult Chat(int? id)
        {


            if (!ReservaExiste(id))
            {
                return RedirectToAction("Index", "Home");
            }

            var reserva = _context.Reserva
                .Include(x => x.Roupa)
                .Include(x => x.Perfil)
                .Include(x => x.Perfil.User)
                .SingleOrDefault(x => x.Id == id);
            if (reserva.Perfil.User.UserName == User.Identity.Name || User.IsInRole("Admin"))
            {
                ViewData["MetodoId"] = new SelectList(ReservasController.GetMetodoDeEnvio(), "value", "metodo", reserva.Envio);
                ViewBag.Id = id;
                return View(reserva);
            }

            return RedirectToAction("Index", "Home");

        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(string? id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();

            var model = new UsersRolesViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Role = (await _userManager.GetRolesAsync(user)).SingleOrDefault(),
                Roles = roles
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string id, UsersRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var oldRole = model.Role;
            // Remove o user de todos os roles
            var result = await _userManager.RemoveFromRolesAsync(user, await _userManager.GetRolesAsync(user));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("Role", "Um erro ococrreu a dar update ao role do utilizador.");
                model.Role = oldRole;
                return View(model);
            }

            // Adicionar o role selecionado ao utilizador
            result = await _userManager.AddToRoleAsync(user, model.Role);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("Role", "Um erro ococrreu a dar update ao role do utilizador.");
                model.Role = oldRole;

                return View(model);
            }

            return RedirectToAction("Index");
        }


        public bool ReservaExiste(int? id)
        {
            if (id == null)
            {
                return false;
            }
            return _context.Reserva.Any(x => x.Id == id);
        }

    }
}
