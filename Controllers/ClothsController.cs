using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using shora.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace ShoraWebsite.Controllers
{
    public class ClothsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostEnvironment _he;


        public ClothsController(ApplicationDbContext context, IHostEnvironment he)
        {
            _context = context;
            _he = he;
        }

        // GET: Cloths
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listagem()
        {
            var applicationDbContext = _context.Roupa.Include(r => r.Categoria);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Cloths/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null || _context.Roupa == null)
            {
                return NotFound();
            }

            var roupa = await _context.Roupa
                .Include(r => r.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            var fotos= roupa.Foto.Split(";");
            ViewData["Fotos"] = fotos;

            if (roupa == null)
            {
                return NotFound();
            }

            return View(roupa);
        }

        // GET: Cloths/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Adicionar()
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adicionar([Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, List<IFormFile> Foto)
        {
            if (ModelState.IsValid)
            {
                if (Foto != null)
                {
                    string destination = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/FotosRoupa/", roupa.Name);

                    if (!Directory.Exists(destination))
                    {
                        Directory.CreateDirectory(destination);
                    }


                    List<string> files = new List<string>();
                    foreach (var f in Foto)
                    {

                        var finalDestination = Path.Combine(destination, f.FileName);
                        FileStream fs = new FileStream(finalDestination, FileMode.Create);
                        f.CopyTo(fs);
                        fs.Close();
                        files.Add(f.FileName);
                    }

                    string s = String.Join(";", files);

                    roupa.Foto = s;
                }





                _context.Add(roupa);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Listagem));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);
            return View(roupa);
        }

        // GET: Cloths/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null || _context.Roupa == null)
            {
                return NotFound();
            }

            var roupa = await _context.Roupa.FindAsync(id);
            if (roupa == null)
            {
                return NotFound();
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);
            ViewData["Foto"] = roupa.Foto;
            return View(roupa);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Name,CategoriaId,Foto")] Roupa roupa)
        {
            if (id != roupa.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    
                    _context.Update(roupa);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoupaExists(roupa.Id))
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
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);
            return View(roupa);
        }

        // GET: Cloths/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Apagar(int? id)
        {
            if (id == null || _context.Roupa == null)
            {
                return NotFound();
            }

            var roupa = await _context.Roupa
                .Include(r => r.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (roupa == null)
            {
                return NotFound();
            }

            return View(roupa);
        }

        // POST: Cloths/Delete/5
        [HttpPost, ActionName("Apagar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApagadoConfirmado(int id)
        {
            if (_context.Roupa == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Roupa'  is null.");
            }
            var roupa = await _context.Roupa.FindAsync(id);


            if (roupa != null)
            {
                string destination = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/FotosRoupa/", roupa.Name);

                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination,true);
                }
                _context.Roupa.Remove(roupa);
            }

            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Listagem));
        }

        private bool RoupaExists(int id)
        {
            return (_context.Roupa?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
