using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using ShoraWebsite.Data.Migrations;

namespace ShoraWebsite.Models
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public ChatHub(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AddToGroup(string reservaId)
        {
            if (int.TryParse(reservaId, out int id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"GroupReservation_{id}");
            }
            else
            {
                return;
            }
        }

        public async Task CheckIfIsVista(string user, string mensagemId)
        {
            if (string.IsNullOrWhiteSpace(user) || !int.TryParse(mensagemId, out int id))
            {
                return;
            }
            var identityUser = await _context.Users.SingleAsync(x => x.UserName == user);

            if (identityUser == null)
            {
                return;

            }

            //aqui achamos o utilizador
            //temos de encontrar agora a mensagem
            var message = await _context.Messages.SingleAsync(x => x.Id == id);
            if (message == null)
            {
                return;
            }
            //encontramos a mensagem



            var userRoles = await _userManager.GetRolesAsync(identityUser);
            var perfil = _context.Perfils.SingleOrDefault(x => x.UserId == identityUser.Id);

            if (perfil == null)
            {
                return;
            }

            //Temos tudo necessario
            bool admin = userRoles.Contains("Admin");

            if (admin)
            {
                message.IsVistaAdmin = true;

            }
            else
            {
                message.IsVistaCliente = true;
            }
            _context.Update(message);
            await _context.SaveChangesAsync();
        }
        public async Task SendMessage(string reservaId, string user, string message)
        {
            var identityUser = _context.Users.Single(x => x.UserName == user);
            if (int.TryParse(reservaId, out int id))
            {
                if (ReservaExiste(id))
                {
                    var userRoles = await _userManager.GetRolesAsync(identityUser);
                    var perfil = _context.Perfils.SingleOrDefault(x => x.UserId == identityUser.Id);

                    if (perfil == null)
                    {
                        return;
                    }


                    bool admin = userRoles.Contains("Admin");


                    var iv = KeyGenerator.GerarIVDaMensagem();

                    var genereratedMessageClass = new Message
                    {
                        ReservaId = id,
                        Text = MessageWrapper.EncryptString(message, perfil.Key, iv),
                        Timestamp = DateTime.Now,
                        UserId = identityUser.Id,
                        IsAdmin = admin,
                        IV = iv
                    };
                    _context.Messages.Add(genereratedMessageClass);

                    await _context.SaveChangesAsync();


                    var tempMessage = genereratedMessageClass;
                    tempMessage.Text = MessageWrapper.DecryptString(genereratedMessageClass.Text, perfil.Key, genereratedMessageClass.IV);
                    tempMessage.IV = "";

                    await Clients.Group($"GroupReservation_{id}").SendAsync("ReceiveMessage", user, genereratedMessageClass.Id, tempMessage);

                }

            }

        }

        //Se esta a ser chamada quer dizer que o user entrou na conversa
        public async Task AskChatHistory(string reservaId, string userName)
        {

            if (int.TryParse(reservaId, out int id))
            {
                if (ReservaExiste(id))
                {
                    var messages = _context.Messages.Include(x => x.User).Where(x => x.ReservaId == id).OrderBy(x => x.Timestamp).ToList();
                   
                    var identityUser = _context.Users.Single(x => x.UserName == userName);

                    var userRoles = await _userManager.GetRolesAsync(identityUser);
                   
                    foreach (var message in messages)
                    {
                        //Temos tudo necessario
                        bool admin = userRoles.Contains("Admin");

                        if (admin)
                        {
                            message.IsVistaAdmin = true;

                        }
                        else
                        {
                            message.IsVistaCliente = true;
                        }
                    }
                    _context.UpdateRange(messages);
                    await _context.SaveChangesAsync();

                    List<Message> messagesDesencriptadas = new List<Message>();

                    foreach (var message in messages)
                    {
                        var tempMessage = message;
                        var perfil = _context.Perfils.SingleOrDefault(x => x.UserId == message.UserId);

                        if (perfil == null)
                        {
                            return;
                        }

                        tempMessage.Text = MessageWrapper.DecryptString(message.Text, perfil.Key, message.IV);
                        tempMessage.IV = "";
                        messagesDesencriptadas.Add(tempMessage);

                    }
                    await Clients.Caller.SendAsync("ReceiveChatHistory", messagesDesencriptadas);

                }

            }
        }

        public bool ReservaExiste(int? id)
        {
            if (id == null)
            {
                return false;
            }
            return _context.Reserva.Any(x => x.Id == id);
        }

    }
}
