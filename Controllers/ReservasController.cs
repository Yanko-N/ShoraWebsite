using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using ShoraWebsite.Models;
using System.Collections;
using System.Data;

namespace ShoraWebsite.Controllers
{
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservasController(ApplicationDbContext context,UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        //Get
        [Authorize(Roles ="Cliente")]
        public async Task<IActionResult> MinhasReservas()
        {
            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;

            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            return View(await _context.Reserva
                .Where(r=>r.Perfil.UserId == user.Id)
           .Include(r => r.Perfil)
           .Include(r => r.Roupa)
           .ToListAsync());

        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Order(string value)
        {

            if (_context.Reserva == null)
            {
                return NotFound();
            }

            ViewBag.Value = value;

            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;

            //0 é ascending - 1 é descending
            switch (value)
            {
                case "0"://Não Vendido
                    return View(await _context.Reserva
               .Include(r => r.Perfil)
               .Include(r => r.Roupa)
               .Where(r => r.Vendida == false)
               .ToListAsync());

                case "1"://Vendido
                    return View(await _context.Reserva
              .Include(r => r.Perfil)
              .Include(r => r.Roupa)
              .Where(r => r.Vendida == true)
              .ToListAsync());

                default:

                    return RedirectToAction(nameof(Listagem));

            }


        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listagem()
        {


            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;



            return View(await _context.Reserva
           .Include(r => r.Perfil)
           .Include(r => r.Roupa)
           .ToListAsync());

        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Listagem(IFormCollection formData)
        {
            string search = formData["search"];

            MetodoDeEnvio[] values = (MetodoDeEnvio[])Enum.GetValues(typeof(MetodoDeEnvio));

            var list = await _context.Reserva
                  .Where(r => r.Perfil.FirstName.Contains(search) || r.Perfil.LastName.Contains(search) || r.Roupa.Name.Contains(search) || r.Tamanho.Contains(search))
            .Include(r => r.Perfil)
            .Include(r => r.Roupa)
            .ToListAsync();

            foreach (var metodo in values)
            {
                if (metodo.ToString().Contains(search))
                {
                    var listComMetodo = _context.Reserva
                        .Include(x=>x.Roupa)
                        .Include(x=>x.Perfil)
                        .Where(x => x.Envio == metodo);

                    foreach (var r in listComMetodo)
                    {
                        if (!list.Contains(r))
                        {
                            list.Add(r);
                        }
                    }
                }
            }

            return View(list);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchVendida(int id, [Bind("Id,RoupaId,PerfilId,Quantidade,Tamanho,Vendida")] Reserva reserva)
        {
            //Lista de mensagens de Erro e de Status
            List<string>? errorsMessages = new List<string>();
            List<string>? statusMessages = new List<string>();

            if (id != reserva.Id || reserva == null || !_context.Roupa.Any(r => r.Id == reserva.RoupaId) || !_context.Reserva.Any(r => r.Id == reserva.Id))
            {

                return NotFound();
            }


            try
            {
                var roupa = _context.Roupa.Find(reserva.RoupaId);
                reserva = _context.Reserva.Find(id);

                reserva.Vendida = !reserva.Vendida;
                _context.Update(reserva);
                await _context.SaveChangesAsync();

                statusMessages.Add($"A venda do material {roupa.Name} - {reserva.Tamanho} foi colocado como {reserva.Vendida}");
            }
            catch (Exception)
            {
                errorsMessages.Add($"Houve um erro a guardar a alteração ");
            }

            if (errorsMessages.Count == 0)
            {
                errorsMessages = null;
            }

            if (statusMessages.Count == 0)
            {
                statusMessages = null;
            }

            TempData["errorsMessages"] = errorsMessages;
            TempData["statusMessages"] = statusMessages;



            return RedirectToAction(nameof(Listagem));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {

            //Lista de mensagens de Erro e de Status
            List<string>? errorsMessages = new List<string>();
            List<string>? statusMessages = new List<string>();

            if (_context.Reserva == null)
            {
                errorsMessages.Add($"Não foi encontrada nenhuma reserva na Base de Dados");
            }
            if (!_context.Reserva.Any(r => r.Id == id))
            {
                errorsMessages.Add($"Não foi encontrada nenhuma reserva com esse id {id} na Base de Dados");

            }

            var reserva = await _context.Reserva.FindAsync(id);


            //Adiciono de novo o stock
            if (_context.StockMaterial != null && _context.StockMaterial.Any(s => s.RoupaId == reserva.RoupaId && s.Tamanho == reserva.Tamanho))
            {
                var stock = _context.StockMaterial.FirstOrDefault(s => s.RoupaId == reserva.RoupaId && s.Tamanho == reserva.Tamanho);
                stock.Quantidade += reserva.Quantidade;
                _context.Update(stock);

            }
            else
            {
                _context.StockMaterial.Add(new Models.Stock()
                {
                    Quantidade = reserva.Quantidade,
                    RoupaId = reserva.RoupaId,
                    Tamanho = reserva.Tamanho
                });
            }


            _context.Reserva.Remove(reserva);
            await _context.SaveChangesAsync();

            statusMessages.Add($"Foi removido com sucesso a reserva {reserva.Roupa}-{reserva.Tamanho} com {reserva.Quantidade}");


            return RedirectToAction(nameof(Listagem));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null || _context.Reserva == null)
            {
                return NotFound();
            }

            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;


            var reserva = await _context.Reserva.FindAsync(id);

            if (reserva == null)
            {
                return NotFound();
            }
            ViewData["MetodoId"] = new SelectList(GetMetodoDeEnvio(), "value", "metodo", reserva.Envio);

            return View(reserva);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [Bind("Id,RoupaId,PerfilId,Quantidade,Tamanho,Envio,Vendida")] Reserva reserva, IFormCollection formData)
        {
            //Lista de erros e de status
            List<string> errorMessages = new List<string>();
            List<string> statusMessages = new List<string>();
            bool invalid = false;

            if (reserva.Id != id || _context.Reserva == null)
            {
                errorMessages.Add($"Não foi encontrada a reserva que procurou");
                invalid = true;
            }

            if (_context.Perfils == null || !_context.Perfils.Any(p => p.Id == reserva.PerfilId))
            {
                errorMessages.Add($"Não foi encontrada o perfil da reserva que procurou");
                invalid = true;

            }


            int quantidadeAntiga = 0;

            //Ja funciona como o try and cacth o serve rn vai abaixo com erro de parsing
            if (int.TryParse(formData["OldQuant"], out int _quantidadeAntiga))
            {
                quantidadeAntiga = _quantidadeAntiga;
            }
            else
            {
                errorMessages.Add($"O valor antigo da quantidade veio no formato errado");
                invalid = true;
            }


            if (reserva.Vendida)
            {
                errorMessages.Add("Não é possivel alterar material já vendido, por favor altere o estado!");
                invalid = true;
            }


            if (ModelState.IsValid && !invalid)
            {

                var perfil = _context.Perfils.Find(reserva.PerfilId);

                //Diferenca entre o valor antigo da reserva e o novo
                //maior que 0 simboliza que o numero de stock diminiu na reserva
                var dif = quantidadeAntiga - reserva.Quantidade;

                //Agora se alteramos a reserva quer dizer que as tshirts alteradas tanto reduzidas como  adicionadas tem de ser alteradas
                if (dif > 0)
                {
                    //Quer dizer que reduziu a quantidade de t shirts logo basta adicionar a DB

                    //Se existe tabela de stock e existe um stock com o id da roupa e o mesmo tamanho
                    if (_context.StockMaterial != null && _context.StockMaterial.Any(s => s.RoupaId == reserva.RoupaId && s.Tamanho == reserva.Tamanho))
                    {
                        //atualizo o stock
                        var stock = _context.StockMaterial.FirstOrDefault(s => s.RoupaId == reserva.RoupaId && s.Tamanho == reserva.Tamanho);
                        stock.Quantidade += dif;
                        _context.Update(stock);
                    }
                    else
                    {
                        //crio um novo stock
                        _context.StockMaterial.Add(new Models.Stock()
                        {
                            Quantidade = dif,
                            RoupaId = reserva.RoupaId,
                            Tamanho = reserva.Tamanho
                        });
                    }

                }
                else if (dif < 0)
                {   //Quer dizer que adicionou mais roupas a reserva do que a quantidade que ja tinha

                    //Verifico se ainda existe stock com o mesma roupa e tamanho
                    if (_context.StockMaterial != null && _context.StockMaterial.Any(s => s.RoupaId == reserva.RoupaId && s.Tamanho == reserva.Tamanho))
                    {
                        
                        
                            //get stock
                            var stock = _context.StockMaterial.FirstOrDefault(s => s.RoupaId == reserva.RoupaId && s.Tamanho == reserva.Tamanho);

                            //se o stock - a quantidade é maior que 0
                            if ((stock.Quantidade += dif) <= 0)
                            {
                                invalid = true;
                                errorMessages.Add($"Não existe stock suficente para adicionar a reserva");

                            }
                            else
                            {
                                _context.Update(stock);

                            }
                        

                    }
                    else
                    {
                        errorMessages.Add($"Não existe stock suficente para adicionar a reserva");
                        invalid = true;
                    }
                }


                if (!invalid)
                {
                    _context.Update(reserva);

                    statusMessages.Add($"A reserva de {perfil.FirstName} {perfil.LastName} foi editada com sucesso");
                    await _context.SaveChangesAsync();



                    TempData["statusMessages"] = statusMessages;

                    return RedirectToAction(nameof(Listagem));
                }

            }

            ViewBag.errorsMessages = errorMessages;
            ViewData["MetodoId"] = new SelectList(GetMetodoDeEnvio(), "value", "metodo", reserva.Envio);

            return View(reserva);
        }
        public static IEnumerable GetMetodoDeEnvio()
        {
            MetodoDeEnvio[] values = (MetodoDeEnvio[])Enum.GetValues(typeof(MetodoDeEnvio));
            var valuesWithMetodos = from value in values
                                    select new { value = (int)value, metodo = value.ToString() };
            return valuesWithMetodos;
        }
    }
}
