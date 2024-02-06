using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using ShoraWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using Humanizer;
using Newtonsoft.Json.Linq;
using System.Collections;

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
            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;

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
                return Problem($"Houve um imprevisto\nErro:{ex}");
            }

        }

        //Get
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Detalhes(int? id)
        {
            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;

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

            List<Stock> stock = new List<Stock>();
            if (_context.StockMaterial != null)
            {
                stock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToListAsync();

            }

            ViewBag.Stock = stock;

            return View(roupa);
        }

        //get
        public async Task<IActionResult> Material(int? id)
        {

            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;


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


            if (!String.IsNullOrWhiteSpace(roupa.Foto))
            {
                ViewData["Fotos"] = roupa.Foto.Split(";");

            }
            else
            {
                ViewData["Fotos"] = null;
            }

            var stocks = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id)
                .ToListAsync();
            ViewData["Stock"] = stocks;

            foreach(var s in stocks)
            {
                roupa.Quantidade += s.Quantidade;
            }

            return View(roupa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
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
                ViewData["Fotos"] = roupa.Foto.Split(";");
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
                ViewBag.errorsMessages = errorMessages;
                TempData["errorsMessages"] = errorMessages;

            }
            else
            {
                ViewBag.errorsMessages = null;
            }

            if (statusMessages.Count != 0)
            {
                ViewBag.statusMessages = statusMessages;
                TempData["statusMessages"] = statusMessages;

            }
            else
            {
                ViewBag.statusMessages = null;
            }

            await _context.AddRangeAsync(reservaList);
            _context.UpdateRange(stockList);
            await _context.SaveChangesAsync();

            if (reservaList.Count > 0)
            {
                
                //AQUI MANDAR MENSAGEM PARA O CLIENTE A PARTIR DE UMA CONTA ADMIN
                var adminsUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var admin = adminsUsers.FirstOrDefault();
                if (admin != null)
                {
                    var perfilAdmin = _context.Perfils.FirstOrDefault(x => x.UserId == admin.Id);
                    if (perfilAdmin != null)
                    {
                        foreach (var reserva in reservaList)
                        {
                            string messageText = $"Olá aqui fala a SHORA, vimos que reservas-te {reserva.Roupa.Name}, diz-nos por aqui como queres que te entreguemos as {reserva.Quantidade} {reserva.Roupa.Categoria.Tipo} e como desejas pagar.";

                            var iv = KeyGenerator.GerarIVDaMensagem();

                            var genereratedMessageClass = new Message
                            {
                                ReservaId = reserva.Id,
                                Text = MessageWrapper.EncryptString(messageText, perfilAdmin.Key, iv),
                                Timestamp = DateTime.Now,
                                UserId = perfilAdmin.UserId,
                                IsAdmin = true,
                                IV = iv,
                                IsVistaAdmin=true,
                                IsVistaCliente=false
                            };
                            _context.Messages.Add(genereratedMessageClass);
                            await _context.SaveChangesAsync();
                            
                        }
                            
                    }
                }
            }

            if(errorMessages.Count == 0)
            {
                return RedirectToAction("MinhasReservas", "Reservas");
            }
            return View(roupa);
        }

        // GET: Cloths/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Adicionar()
        {
            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;


            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adicionar([Bind("Id,Name,CategoriaId,Preco,Foto")] Roupa roupa, List<IFormFile> Foto, IFormCollection formData)
        {

            bool invalid = false;
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
                    ViewBag.errorsMessages = errorMessages;
                    return View(roupa);
                }

            }
            catch (Exception)
            {
                errorMessages.Add($"O valor inserido no Preco é Inválido");
                ViewBag.errorsMessages = errorMessages;
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


                        if (int.TryParse(quantArray[i], out int quantidade) && quantidade > 0)
                        {
                            stockList.Add(new Stock
                            {
                                Quantidade = quantidade,
                                Tamanho = sizes[i],
                                Roupa = roupa,
                                RoupaId = roupa.Id
                            });
                            statusMessages.Add($"Foi adicionado com sucesso {quantidade} a peca {roupa.Name} - {sizes[i]}");
                        }
                        else
                        {
                            errorMessages.Add($"Quantidade inserida da peca {roupa.Name} - {sizes[i]}  tem de ser do formato inteiro e maior que 0");
                            invalid = true;

                        }

                    }

                }



                //Guardo na DB o Stock Total
                await _context.StockMaterial.AddRangeAsync(stockList);
                //Adiciono a DB a roupa
                _context.Add(roupa);



                //DB guarda as mudanças
                await _context.SaveChangesAsync();




                //Houve erros na criação da roupa
                if (!invalid)
                {
                    TempData["errorsMessages"] = errorMessages;
                    TempData["statusMessages"] = statusMessages;
                    return RedirectToAction(nameof(Detalhes), new { id = roupa.Id });

                }
                else
                {
                    //Adicionar erros
                    ViewBag.errorsMessages = errorMessages;
                    ViewBag.statusMessages = statusMessages;
                    return RedirectToAction(nameof(Editar), new { id = roupa.Id });
                }


            }
            else
            {
                //recebo as mensagens de Erro
                var errors = ModelState.Values
                   .SelectMany(x => x.Errors)
                   .Select(x => x.ErrorMessage);

                errorMessages.AddRange(errors);

                ViewBag.errorsMessages = errorMessages;
                return View(roupa);
            }

        }


        //Get
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int? id)
        {
            ViewData["statusMessages"] = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewData["errorsMessages"] = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;

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
            TempData["Price"] = roupa.Preco.ToString();

            return View(roupa);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Name,CategoriaId,Foto")] Roupa roupa, IFormCollection formData)
        {
            //Bool para checkar se afinal
            bool invalid = false;

            if (roupa.Id != id)
            {
                return NotFound();
            }

            if (_context.StockMaterial == null)
            {
                return Problem("Na base de Dados não existe stock de material");
            }

            //Lista de erros e de status
            List<string> errorMessages = new List<string>();
            List<string> statusMessages = new List<string>();





            float? oldPrice = null;
            ViewBag.Stock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToListAsync();
            ViewData["CategoriaId"] = new SelectList(_context.Categoria, "Id", "Tipo", roupa.CategoriaId);
            ViewData["statusMessages"] = statusMessages;




            try
            {

                if (TempData.TryGetValue("Price", out var oldPriceConverted))
                {
                    if (float.TryParse((string?)oldPriceConverted, out float TrueOldPrice))
                    {
                        oldPrice = TrueOldPrice;
                    }
                    else
                    {
                        errorMessages.Add($"Houve alterações inválidas no preço antigo ");
                        ViewBag.errorsMessages = errorMessages;
                        return View(roupa);
                    }
                }
            }
            catch (Exception)
            {
                errorMessages.Add($"Houve alterações inválidas no preço antigo ");
                ViewBag.errorsMessages = errorMessages;
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
                    ViewBag.errorsMessages = errorMessages;


                    ViewBag.Price = oldPrice;

                    return View(roupa);
                }

            }
            catch (Exception)
            {
                errorMessages.Add($"O valor inserido no Preco é Inválido");
                ViewBag.errorsMessages = errorMessages;

                ViewBag.Price = oldPrice;
                return View(roupa);
            }


            //Adcionar a viewBag o preço antigo
            ViewBag.Price = oldPrice;

            if (ModelState.IsValid)
            {
                if (formData["oldName"].ToString() != roupa.Name)
                {
                    //O nome do produto foi alterado por isso vamos ter que mudar o nome da pasta a qual está guardado
                    string oldPath = Path.Combine(_he.ContentRootPath, "wwwroot", "Documentos", "FotosRoupa", formData["oldName"].ToString());
                    string newPath = Path.Combine(_he.ContentRootPath, "wwwroot", "Documentos", "FotosRoupa", roupa.Name);

                    if (Directory.Exists(oldPath))
                    {
                        Directory.Move(oldPath, newPath);
                    }
                    
                }

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
                    if (!String.IsNullOrWhiteSpace(quantArray[i]))
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
                            errorMessages.Add($"No tamanho {sizes[i]} o valor inserido não é um inteiro");
                            invalid = true;

                        }

                    }
                }


                //Atualizar a DB
                _context.AddRange(newStocks);
                _context.UpdateRange(stockDB);
                _context.Update(roupa);
                await _context.SaveChangesAsync();


                if (!invalid)
                {
                    TempData["statusMessages"] = statusMessages;
                    return RedirectToAction(nameof(Detalhes), new { id = roupa.Id });
                }
                else
                {

                    ViewBag.statusMessages = statusMessages;
                    ViewBag.errorsMessages = errorMessages;

                    return View(roupa);
                }

            }

           

            return View(roupa);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            //Lista de erros e de status
            List<string> errorMessages = new List<string>();
            List<string> statusMessages = new List<string>();

            if (_context.Roupa == null && !RoupaExists(id))
            {
                return Problem("A base de Dados não contém Roupa nenhuma");
            }

            var roupa = await _context.Roupa.FindAsync(id);
            try
            {

                string destination = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/FotosRoupa/", roupa.Name);

                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, true);
                }
                _context.Roupa.Remove(roupa);



                //Vamos deletar também o stock que tem a roupa

                var listStock = await _context.StockMaterial.Where(s => s.RoupaId == roupa.Id).ToArrayAsync();

                _context.StockMaterial.RemoveRange(listStock);



                //APAGAR TB AS RESERVAS COM A T SHIRT???

                var listReserva = await _context.Reserva.Where(r => r.RoupaId == roupa.Id).ToArrayAsync();
                _context.Reserva.RemoveRange(listReserva);

                statusMessages.Add($"A peça {roupa.Name} foi removida com sucesso");

                TempData["errorsMessages"] = errorMessages;
                TempData["statusMessages"] = statusMessages;


                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                errorMessages.Add($"A peça {roupa.Name} não foi removida com sucesso");

                TempData["errorsMessages"] = errorMessages;
                TempData["statusMessages"] = statusMessages;

            }



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
