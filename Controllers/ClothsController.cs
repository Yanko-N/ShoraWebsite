using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using shora.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ShoraWebsite.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace ShoraWebsite.Controllers
{
    public class ClothsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostEnvironment _he;
        private readonly UserManager<IdentityUser> _userManager;

        [TempData]
        public string StatusMessage { get; set; }

        public ClothsController(ApplicationDbContext context, IHostEnvironment he, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _he = he;
            _userManager = userManager;
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

            if (roupa == null)
            {
                return NotFound();
            }

            var fotos = roupa.Foto.Split(";");

            var stock = await _context.StockMaterial.Where(s => s.RoupaId == id)
                .ToListAsync();

            ViewData["Fotos"] = fotos;
            ViewData["Stock"] = stock;


            return View(roupa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Material([Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, IFormCollection formData)
        {

            if (roupa.Id.ToString() != formData["roupaId"])
            {
                return NotFound();
            }


            int? perfilID;

            try
            {
                var userId = _userManager.GetUserId(User);
                var perfil = await _context.Perfils.FirstOrDefaultAsync(p => p.UserId == userId);
                perfilID = perfil.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ": Error");
                return Error();
            }


            var reservaList = new List<Reserva>();
            string[] quantArray = { formData["Quant_XS"], formData["Quant_S"], formData["Quant_M"], formData["Quant_L"], formData["Quant_XL"], formData["Quant_XXL"] };


            string[] sizes = { "XS", "S", "M", "L", "XL", "XXL" };
            var stockList = new List<Stock>();
            for (int i = 0; i < quantArray.Length; i++)
            {
                if (quantArray[i] != null)
                {
                    try
                    {

                        int x = 0;
                        if (!String.IsNullOrEmpty(quantArray[i]))
                        {
                            x = int.Parse(quantArray[i]);
                        }

                        if (x > 0)
                        {
                            if (perfilID == null) return Problem("O perfil não existe");

                            var stock = _context.StockMaterial.FirstOrDefault(s => s.Roupa.Id == roupa.Id && s.Tamanho == sizes[i]);
                            if (stock.Quantidade - x < 0)
                            {

                                ///VERIFICAR ISTO DO STATUSMESSAGE
                                StatusMessage = "Não existe Stock suficiente ";

                                ModelState.AddModelError(nameof(roupa.Quantidade), "Não existe Stock suficiente ");

                            }

                            if (!ModelState.IsValid)
                            {


                                if (roupa != null)
                                {
                                    Roupa roupaHelper = await _context.Roupa
                                    .Include(r => r.Categoria)
                                    .FirstOrDefaultAsync(r => r.Id == roupa.Id);

                                    if (roupaHelper != null)
                                    {
                                        roupa=roupaHelper;
                                    }

                                    var fotos = roupaHelper.Foto.Split(";");
                                    ViewData["Fotos"] = fotos;


                                    var stockPage = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
                                            .ToListAsync();
                                    ViewData["Stock"] = stockPage;

                                    return View(roupa);

                                }
                                else
                                {
                                    return Problem("A roupa não existe");
                                }
                            }

                            stock.Quantidade -= x;
                            stockList.Add(stock);

                            reservaList.Add(new Reserva
                            {
                                Quantidade = x,
                                RoupaId = roupa.Id,
                                Vendida = false,
                                PerfilId = (int)perfilID,
                                Tamanho = sizes[i]

                            });


                        }


                    }
                    catch (Exception ex)
                    {
                        return Problem("Encontramos um problema :" + ex);
                    }
                }
            }


            await _context.AddRangeAsync(reservaList);
            _context.UpdateRange(stockList);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Listagem));
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

                            int x = 0;
                            if (!String.IsNullOrEmpty(quantArray[i]))
                            {
                                x = int.Parse(quantArray[i]);
                            }

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
                            return Problem("Encontramos um problema :" + ex);

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


            var stock = await _context.StockMaterial.Where(s => s.RoupaId == id)
                .ToListAsync();

            ViewData["Stock"] = stock;

            return View(roupa);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, IFormCollection formData)
        {
            if (id != roupa.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {


                    var stockList = new List<Stock>();
                    string[] quantArray = { formData["Quant_XS"], formData["Quant_S"], formData["Quant_M"], formData["Quant_L"], formData["Quant_XL"], formData["Quant_XXL"] };


                    string[] sizes = { "XS", "S", "M", "L", "XL", "XXL" };

                    for (int i = 0; i < quantArray.Length; i++)
                    {
                        if (quantArray[i] != null)
                        {


                            try
                            {
                                int x = 0;
                                if (String.IsNullOrEmpty(quantArray[i]))
                                {
                                    var new_s = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id && s.Tamanho == sizes[i]).FirstOrDefaultAsync();
                                    if (new_s != null) // Quer dizer que o user adicionou um valor em nulo ou vazio e esse stock nao existe
                                    {
                                        x = new_s.Quantidade;
                                    }


                                }
                                else
                                {
                                    x = int.Parse(quantArray[i]);
                                }





                                if (x >= 0)
                                {
                                    var s = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id && s.Tamanho == sizes[i]).FirstOrDefaultAsync();

                                    if (s != null)
                                    {
                                        s.Quantidade = x;

                                        _context.Update(s);
                                    }
                                    else
                                    {
                                        _context.Add(new Stock
                                        {
                                            RoupaId = roupa.Id,
                                            Roupa = roupa,
                                            Quantidade = x,
                                            Tamanho = sizes[i]
                                        });
                                    }

                                }



                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex + " :Error");
                                return Error();
                            }

                        }
                    }
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Roupa == null)
            {
                return Problem("A base de Dados não contém Roupa nenhuma");
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


            //Vamos deletar também o stock que tem a roupa

            var listStock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToArrayAsync();

            _context.StockMaterial.RemoveRange(listStock);



            //TEMOS DE APAGAR TB AS RESERVAS COM A T SHIRT???

            var listReserva = await _context.Reserva.Where(r => r.RoupaId == roupa.Id).ToArrayAsync();
            _context.Reserva.RemoveRange(listReserva);



            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Listagem));
        }

        private bool RoupaExists(int id)
        {
            return (_context.Roupa?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
