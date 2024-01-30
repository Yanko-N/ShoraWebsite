using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using ShoraWebsite.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace ShoraWebsite.Areas.Identity.Pages.Account.Manage
{
    public class MyChatModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;
        private readonly ApplicationDbContext _context;

        public MyChatModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<ChangePasswordModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }



        //Here Only Clients are going to enter in this page


        public async Task<IActionResult> OnGetAsync()
        {
            if (User.IsInRole("Cliente"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                }

                var mineMessages = _context.Messages.Where(m => m.SenderId == user.UserName);
                var targetMessages = _context.Messages.Where(m => m.TargetId == user.UserName);

                List<Message> messages = new List<Message>();
                messages.AddRange(mineMessages);
                messages.AddRange(targetMessages);

                
                Conversa conversa = new Conversa() {
                    MineMessages = mineMessages.ToList(),
                    TargetMessages = targetMessages.ToList(),
                    AllMensagens = messages.OrderBy(m=>m.TimeSended).ToList()
                };

                ViewData["Model"] = conversa;
                ViewData["UserName"] = user.UserName.Split("-")[0];
                   return Page();

            }
            else
            {
                //Basically if a user that is not a client is not going to receve the page
                return NotFound();
            }
        }

    
    }
}
