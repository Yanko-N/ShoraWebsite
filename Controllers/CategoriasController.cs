using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using shora.Models;
using Microsoft.AspNetCore.Authorization;

namespace ShoraWebsite.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categorias
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Listagem()
        {

            return View(await _context.Categoria.ToListAsync());
                          
        }


        // GET: Categorias/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Adicionar()
        {
            return View();
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adicionar([Bind("Id,Tipo")] Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                if(!_context.Categoria.Any(c=>c.Tipo == categoria.Tipo))
                {
                    _context.Add(categoria);
                    await _context.SaveChangesAsync();
                }
                
                return RedirectToAction(nameof(Listagem));
            }
            return View(categoria);
        }

        // GET: Categorias/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null || _context.Categoria == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            return View(categoria);
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Tipo")] Categoria categoria)
        {
            if (id != categoria.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoriaExists(categoria.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Listagem));
            }
            return View(categoria);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Categoria == null)
            {
                return Problem("A base de dados não contém Categoria nenhuma.");
            }
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria != null)
            {
                _context.Categoria.Remove(categoria);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Listagem));

        }
     
        private bool CategoriaExists(int id)
        {
          return (_context.Categoria?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
