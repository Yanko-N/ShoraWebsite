using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shora.Models;
using ShoraWebsite.Data;
using System.Data;

namespace ShoraWebsite.Controllers
{
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listagem(string? search)
        {
            

            if (TempData.TryGetValue("statusMessages", out var statusMessages))
            {
                ViewBag.statusMessages = statusMessages;
            }
            else
            {
                ViewBag.statusMessages = null;

            }

            if (TempData.TryGetValue("errorsMessages", out var errorMessages))
            {
                ViewBag.errorMessages = errorMessages;
            }
            else
            {
                ViewBag.errorMessages = null;

            }

            if(search == null)
            {
                return View(await _context.Reserva.
                Include(r => r.Perfil)
                .Include(r => r.Roupa)
                .ToListAsync());
            }
            else
            {

               
                return View(await _context.Reserva.
                               Include(r => r.Perfil)
                               .Include(r => r.Roupa)
                               .Where(r=>r.Perfil.FirstName.Contains(search) || r.Perfil.LastName.Contains(search))
                               .ToListAsync());
            }
            

        }




        //Esta sempre a entrar como falso o parametro Vendida
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchVendida(int id, [Bind("Id,RoupaId,PerfilId,Quantidade,Tamanho,Vendida")] Reserva reserva)
        {
            //Lista de mensagens de Erro e de Status
            List<string>? errorsMessages = new List<string>();
            List<string>? statusMessages = new List<string>();

            if (id != reserva.Id || reserva == null || !_context.Roupa.Any(r=>r.Id == reserva.RoupaId) || !_context.Reserva.Any(r=>r.Id == reserva.Id))
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

            if(errorsMessages.Count == 0)
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
    }
}
