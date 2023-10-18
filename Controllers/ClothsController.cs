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
using System.Collections.Generic;

namespace ShoraWebsite.Controllers
{
    public class ClothsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostEnvironment _he;
        private readonly UserManager<IdentityUser> _userManager;



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

            var stock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
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

            if (TempData.TryGetValue("statusMessages", out var statusMessages))
            {
                ViewBag.statusMessages = statusMessages;
            }

            if (TempData.TryGetValue("errorsMessages", out var errorMessages))
            {
                ViewBag.errorMessages = errorMessages;
            }

            var roupa = await _context.Roupa
                .Include(r => r.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (roupa == null)
            {
                return NotFound();
            }

            var fotos = roupa.Foto.Split(";");

            var stock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
                .ToListAsync();

            ViewData["Fotos"] = fotos;
            ViewData["Stock"] = stock;
            ViewBag.errorMessage = null;
            ViewBag.statusMessage = null;

            return View(roupa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Material([Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, IFormCollection formData)
        {
            //verifico se o id passado é igual ao do form e vou buscar a roupa e verifico se a roupa existe na DB
            if (roupa.Id.ToString() != formData["roupaId"] || (roupa = await _context.Roupa.Include(r => r.Categoria).FirstOrDefaultAsync(r => r.Id == roupa.Id)) == null)
            {
                return Problem("Não foi encrontrada a roupa");
            }




            if (!TryGetUserPerfil(out int? perfilID))
            {
                return Problem("Não foi encontrado o utilizador");
            }


            if (!TryGetRoupaFromDatabase(roupa.Id, out roupa))
            {
                return Problem("Não foi encrontrada a roupa");

            }



            //Recebo os parametros do Form
            string[] quantArray = { formData["Quant_XS"], formData["Quant_S"], formData["Quant_M"], formData["Quant_L"], formData["Quant_XL"], formData["Quant_XXL"] };

            //Tamanhos existentes
            string[] sizes = { "XS", "S", "M", "L", "XL", "XXL" };

            //Lista de stock para depois atualizar na DB
            var stockList = new List<Stock>();
            //Crio uma lista de Reservas para depois adicionar a DB
            List<Reserva> reservaList = new List<Reserva>();

            //Listas de Mensagens de Erro e de Status
            List<string> errorMessages = new List<string>();
            List<string> statusMessages = new List<string>();


            //Passo por todos os tamanhos recebidos e por seu Tamanho
            for (int i = 0; i < quantArray.Length; i++)
            {
                //Se for diferente de nulo vou processar
                if (quantArray[i] != null)
                {

                    //Tentar fazer Parsing
                    try
                    {
                        if (int.TryParse(quantArray[i], out int quantidade) && quantidade > 0)
                        {
                            Stock stock;
                            if (TryGetStock(roupa.Id, sizes[i], out stock))
                            {
                                //Se por ventura o utilizador tentar comprar mais material que existe na DB
                                if (stock.Quantidade - quantidade >= 0)
                                {

                                    //Temos Stock para processar a reserva

                                    stock.Quantidade -= quantidade;
                                    //Verifico se acabou com o stock
                                    if (stock.Quantidade == 0)
                                    {
                                        _context.StockMaterial.Remove(stock);
                                    }
                                    else
                                    {
                                        stockList.Add(stock); //adiciono a lista de stock para ser alterado
                                    }

                                    //Adiciono a Reserva a DB
                                    reservaList.Add(new Reserva
                                    {
                                        Quantidade = quantidade,
                                        RoupaId = roupa.Id,
                                        Vendida = false,
                                        PerfilId = (int)perfilID,
                                        Tamanho = sizes[i]

                                    });
                                    //Adiciono a Status Message
                                    statusMessages.Add($"Foram reservados {quantidade} {roupa.Name} {sizes[i]}");
                                }
                                else
                                {
                                    errorMessages.Add($"Houve um imprevisto, não temos stock suficiente para a reserva submetida de {roupa.Name}-{sizes[i]}");
                                }
                            }
                        }
                        else
                        {
                            errorMessages.Add($"Houve erro com o Input de {sizes[i]} de {roupa.Name}, logo não foi possivel reservar este tamanho");
                        }
                    }
                    catch (Exception ex)
                    {

                        return Problem("Encontramos um problema :" + ex);
                    }
                }
            }

            var stockPage = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToListAsync();
            var fotos = roupa.Foto.Split(";");

            ViewData["Stock"] = stockPage;
            ViewData["Fotos"] = fotos;



            if (errorMessages.Count != 0)
            {
                ViewBag.errorMessages = errorMessages;

            }
            else
            {
                ViewBag.errorMessages = null;
            }

            if (statusMessages.Count != 0)
            {
                ViewBag.statusMessages = statusMessages;

            }
            else
            {
                ViewBag.statusMessages = null;
            }

            await _context.AddRangeAsync(reservaList);
            _context.UpdateRange(stockList);
            await _context.SaveChangesAsync();

            return View(roupa);
        }


        private bool TryGetUserPerfil(out int? perfilID)
        {
            perfilID = null;
            try
            {
                var userId = _userManager.GetUserId(User);
                var perfil = _context.Perfils.FirstOrDefault(p => p.UserId == userId);
                if (perfil != null)
                {
                    perfilID = perfil.Id;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetRoupaFromDatabase(int roupaId, out Roupa roupa)
        {
            roupa = _context.Roupa.Include(r => r.Categoria).FirstOrDefault(r => r.Id == roupaId);
            return roupa != null;
        }

        private bool TryGetStock(int roupaId, string size, out Stock stock)
        {
            stock = _context.StockMaterial.FirstOrDefault(s => s.Roupa.Id == roupaId && s.Tamanho == size);
            return stock != null;
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
        public async Task<IActionResult> Adicionar([Bind("Id,Name,CategoriaId,Preco,Foto")] Roupa roupa, List<IFormFile> Fotos, IFormCollection formData)
        {
            //Lista de erros e de status
            List<string> errorMessages = new List<string>();
            List<string>  statusMessages = new List<string>();

            //Inicializar primeiro a variavel
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);


            if (ModelState.IsValid)
            {

               
                if (Fotos != null)
                {
                    string destination = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/FotosRoupa/", roupa.Name);

                    //Verifico se existe já esse caminho
                    if (!Directory.Exists(destination))
                    {
                        //crio
                        Directory.CreateDirectory(destination);
                    }

                    //Lista de string dos ficheiros para guardar na DB
                    List<string> files = new List<string>();

                    //Passo por todas as Fotos Recebidas
                    foreach (var f in Fotos)
                    {
                        //Destino Final
                        var finalDestination = Path.Combine(destination, f.FileName);

                        //Crio uma Stream nesse caminho,coloco-o lá e fecho
                        FileStream fs = new FileStream(finalDestination, FileMode.Create);
                        f.CopyTo(fs);
                        fs.Close();

                        //Adiciona a lista de strings de ficheiros
                        files.Add(f.FileName);
                    }

                    //String que contém todos os ficheiros
                    string s = String.Join(";", files);

                    //Foto quarda todos os nomes dos ficheiros
                    roupa.Foto = s;
                }




                //Lista de Stock
                var stockList = new List<Stock>();

                //Array com os parametros recebidos do Form
                //Guarda a quantidade recebida de cada tamanho
                string[] quantArray = { formData["Quant_XS"], formData["Quant_S"], formData["Quant_M"], formData["Quant_L"], formData["Quant_XL"], formData["Quant_XXL"] };

                //Tamanhos Disponiveis
                string[] sizes = { "XS", "S", "M", "L", "XL", "XXL" };

                //Percorro por todos as quantidades recebidas
                for (int i = 0; i < quantArray.Length; i++)
                {
                    //Se realmente veio algum parametro de input
                    if (!String.IsNullOrEmpty(quantArray[i]))
                    {
                        try
                        {
                            if (int.TryParse(quantArray[i],out int quantidade) && quantidade > 0)
                            {
                                stockList.Add(new Stock
                                {
                                    Quantidade = quantidade,
                                    Tamanho = sizes[i],
                                    Roupa = roupa,
                                    RoupaId = roupa.Id
                                });
                            }
                           
                        }
                        catch (Exception)
                        {
                            errorMessages.Add($"Quantidade inserida no tamanho {sizes[i]} é um valor inválido.");

                        }
                    }
                    else
                    {
                        errorMessages.Add($"Quantidade inserida no tamanho {sizes[i]} veio vazia.");
                    }

                }
                TempData["errorsMessages"] = errorMessages;
                TempData["statusMessages"] = statusMessages;
                //Guardo na DB o Stock Total
                await _context.StockMaterial.AddRangeAsync(stockList);
                //Adiciono a DB a roupa
                _context.Add(roupa);



                //DB guarda as mudanças
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Material), new { id = roupa.Id });
            }
            else
            {
                //recebo as mensagens de Erro
                var errors = ModelState.Values
                   .SelectMany(x => x.Errors)
                   .Select(x => x.ErrorMessage);

                errorMessages.AddRange(errors);

                ViewBag.errorMessages = errorMessages;
                return View(roupa);
            }
            
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


            var stock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
                .ToListAsync();

            ViewData["Stock"] = stock;

            return View(roupa);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, IFormCollection formData)
        {
            if (roupa.Id != roupa.Id)
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
