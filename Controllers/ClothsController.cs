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
using Humanizer;

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


                TempData["statusMessages"] = null;
                TempData["errosMessages"] = null;

                return View(roupas);

            }
            catch (Exception ex)
            {
                return Problem($"Houve um imprevisto\nErro:{ex}");
            }

        }

        //Get
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


            if (roupa == null)
            {
                return NotFound();
            }


            if (String.IsNullOrWhiteSpace(roupa.Foto))
            {
                ViewData["Fotos"] = null;
            }
            else
            {
                ViewData["Fotos"] = roupa.Foto.Split(";");
            }


            ViewData["Stock"] = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
                .ToListAsync();

            return View(roupa);
        }

        //get
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


            if (!String.IsNullOrWhiteSpace(roupa.Foto))
            {
                ViewData["Fotos"] = roupa.Foto.Split(";");

            }
            else
            {
                ViewData["Fotos"] = null;
            }


            ViewData["Stock"] = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
                .ToListAsync();


            return View(roupa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Material([Bind("Id,Name,CategoriaId,Preco,Foto")] Roupa roupa, IFormCollection formData)
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

            //Array com as strings das fotos
            
            if (String.IsNullOrWhiteSpace(roupa.Foto))
            {
                ViewData["Fotos"] = null;
            }
            else
            {
                ViewData["Fotos"] =  roupa.Foto.Split(";");
            }

            //Passo por todos os tamanhos recebidos e por seu Tamanho
            for (int i = 0; i < quantArray.Length; i++)
            {
                //Se for diferente de nulo vou processar
                if (!String.IsNullOrWhiteSpace(quantArray[i]))
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

            ViewData["Stock"] = stockPage;
           



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

        // GET: Cloths/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Adicionar()
        {
            if (TempData.TryGetValue("statusMessages", out var statusMessages))
            {
                ViewBag.statusMessages = statusMessages;
            }

            if (TempData.TryGetValue("errorsMessages", out var errorMessages))
            {
                ViewBag.errorMessages = errorMessages;
            }


            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adicionar([Bind("Id,Name,CategoriaId,Preco,Foto")] Roupa roupa, List<IFormFile> Foto, IFormCollection formData)
        {
            //Lista de erros e de status
            List<string> errorMessages = new List<string>();
            List<string> statusMessages = new List<string>();

            //Inicializar primeiro a variavel
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);

            try
            {
                if (float.TryParse(formData["Preco"], out float preco))
                {
                    roupa.Preco = preco;
                }
                else
                {
                    errorMessages.Add($"O valor inserido no Preco não é no formato correto - 12,4 ");
                    ViewBag.errorMessages = errorMessages;
                    return View(roupa);
                }

            }
            catch (Exception)
            {
                errorMessages.Add($"O valor inserido no Preco é Inválido");
                ViewBag.errorMessages = errorMessages;
                return View(roupa);
            }

            if (ModelState.IsValid)
            {


                if (Foto != null)
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
                    foreach (var f in Foto)
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
                    if (!String.IsNullOrWhiteSpace(quantArray[i]))
                    {
                        try
                        {
                            if (int.TryParse(quantArray[i], out int quantidade) && quantidade > 0)
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
                            errorMessages.Add($"Quantidade inserida no tamanho {sizes[i]} tem de ser do formato \" 1,23 \" .");

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

                //Houve erros na criação da roupa
                if (errorMessages.Count > 0)
                {
                    return RedirectToAction(nameof(Editar), new { id = roupa.Id });
                }
                else
                {
                    return RedirectToAction(nameof(Material), new { id = roupa.Id });
                }


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

    
        //Get
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int? id)
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
            TempData["Price"] = roupa.Preco.ToString();
            return View(roupa);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Name,CategoriaId,Preco,Foto")] Roupa roupa, IFormCollection formData)
        {
            if (roupa.Id != id)
            {
                return NotFound();
            }

            //Lista de erros e de status
            List<string> errorMessages = new List<string>();
            List<string> statusMessages = new List<string>();

            ViewBag.Stock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToListAsync();
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);

            try
            {

                if (TempData.TryGetValue("Price", out var oldPrice))
                {
                    if (float.TryParse((string?)oldPrice, out float oldPriceConverted))
                    {
                        roupa.Preco = oldPriceConverted;
                    }
                    else
                    {
                        errorMessages.Add($"Houve alterações inválidas no preço antigo ");
                        ViewBag.errorMessages = errorMessages;
                        return View(roupa);
                    }
                }
            }
            catch (Exception)
            {
                errorMessages.Add($"Houve alterações inválidas no preço antigo ");
                ViewBag.errorMessages = errorMessages;
                return View(roupa);
            }


            try
            {
                if (float.TryParse(formData["Preco"], out float preco))
                {
                    roupa.Preco = preco;
                }
                else
                {
                    errorMessages.Add($"O valor inserido no Preco não é no formato correto - 12,4 ");
                    ViewBag.errorMessages = errorMessages;
                    return View(roupa);
                }

            }
            catch (Exception)
            {
                errorMessages.Add($"O valor inserido no Preco é Inválido");
                ViewBag.errorMessages = errorMessages;
                return View(roupa);
            }


            if (ModelState.IsValid)
            {

                //Lista do Stock inserido
                var stockList = new List<Stock>();

                //Guardar os parametros recebidos pelo form
                string[] quantArray = { formData["Quant_XS"], formData["Quant_S"], formData["Quant_M"], formData["Quant_L"], formData["Quant_XL"], formData["Quant_XXL"] };

                //Tamanhos existentes
                string[] sizes = { "XS", "S", "M", "L", "XL", "XXL" };

                //Lista de stock que existe na DB
                List<Stock> stockDB = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToListAsync();

                //Lista de novos stocks para serem adcionados a DB
                List<Stock> newStocks = new List<Stock>();

                //Loop pela quantidade recebida
                for (int i = 0; i < quantArray.Length; i++)
                {
                    if (String.IsNullOrEmpty(quantArray[i]))
                    {

                        try
                        {
                            if (int.TryParse(quantArray[i], out int quantidade))
                            {
                                //stock temporario
                                var tempStock = stockDB.FirstOrDefault(stock => stock.Tamanho == sizes[i]);

                                if (tempStock != null) //Quer dizer que  existe já o stock na DB
                                {
                                    //Alteramos a quantidade
                                    tempStock.Quantidade = quantidade;
                                    statusMessages.Add($"Foi alterada a quantidade peças de {roupa.Name} - {sizes[i]} para {quantidade} ");
                                }
                                else
                                {
                                    Stock newStock = new()
                                    {
                                        RoupaId = roupa.Id,
                                        Roupa = roupa,
                                        Quantidade = quantidade,
                                        Tamanho = sizes[i]
                                    };
                                    //Adiciono a lista de novo stock
                                    newStocks.Add(newStock);

                                    statusMessages.Add($"Foi adicionada {quantidade} peças de {roupa.Name} - {sizes[i]}");
                                }
                            }
                            else
                            {
                                errorMessages.Add($"No tamanho {sizes[i]} o valor inserido é invalido");

                            }
                        }
                        catch (Exception)
                        {
                            errorMessages.Add($"No tamanho {sizes[i]} houve um erro");
                        }

                    }
                }



                //Atualizar a DB
                _context.AddRange(newStocks);
                _context.UpdateRange(stockDB);
                _context.Update(roupa);
                await _context.SaveChangesAsync();

                TempData["statusMessages"] = statusMessages;
                TempData["errorMessages"] = errorMessages;

                return RedirectToAction(nameof(Material), new { id = roupa.Id });
            }

          
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

    }
}
