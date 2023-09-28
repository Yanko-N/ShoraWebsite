using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using shora.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ShoraWebsite.Models;
using System.Diagnostics;

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
            try
            {
                var stock = await _context.StockMaterial.Include(r => r.Roupa)
                    .OrderBy(s => s.Roupa.CategoriaId)
                    .ToArrayAsync();

                var roupas = await _context.Roupa.Include(r => r.Categoria).ToListAsync();

                foreach (var s in stock)
                {
                    foreach (var r in roupas)
                    {
                        if (s.RoupaId == r.Id)
                        {
                            r.Quantidade += s.Quantidade;
                        }
                    }
                }


                return View(roupas);

            }
            catch (Exception ex)
            {
                return Error();
            }

        }

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

            var fotos = roupa.Foto.Split(";");

            var stock = await _context.StockMaterial.Where(s => s.RoupaId == id)
                .ToListAsync();

            ViewData["Fotos"] = fotos;
            ViewData["Stock"] = stock;
            if (roupa == null)
            {
                return NotFound();
            }

            return View(roupa);
        }


        public async Task<IActionResult> Material(int? id)
        {

            if (id == null || _context.Roupa == null)
            {
                return NotFound();
            }

            var roupa = await _context.Roupa
                .Include(r => r.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            var fotos = roupa.Foto.Split(";");

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
        public async Task<IActionResult> Adicionar([Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, List<IFormFile> Foto, IFormCollection formData)
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





                var stockList = new List<Stock>();
                string[] quantArray = { formData["Quant_XS"], formData["Quant_S"], formData["Quant_M"], formData["Quant_L"], formData["Quant_XL"], formData["Quant_XXL"] };


                string[] sizes = { "XS", "S", "M", "L", "XL", "XXL" };

                for (int i = 0; i < quantArray.Length; i++)
                {
                    if (quantArray[i] != null)
                    {
                        try
                        {

                            int x = int.Parse(quantArray[i]);

                            if (x > 0)
                            {
                                stockList.Add(new Stock
                                {
                                    Quantidade = x,
                                    Tamanho = sizes[i],
                                    Roupa = roupa,
                                    RoupaId = roupa.Id
                                });
                            }


                            Console.WriteLine("One added");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex + " :Error");
                            return Error();
                        }
                    }

                }

                foreach (var s in stockList)
                {
                    _context.Add(s);
                }

                _context.Add(roupa);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Listagem));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);
            return View(roupa);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
                    Directory.Delete(destination, true);
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
