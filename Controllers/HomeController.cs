
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoraWebsite.Data;
using ShoraWebsite.Models;
using System.Text.Json;
using System.Diagnostics;
using System.Buffers.Text;
using System.Web;

namespace ShoraWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userM;
        private readonly IHostEnvironment _he;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userM, IHostEnvironment he)
        {
            _logger = logger;
            _context = context;
            _userM = userM;
            _he = he;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFoto(List<IFormFile> Foto)
        {
            List<string> errorsMessages = new List<string>();
            List<string> statusMessages = new List<string>();



            if (Foto != null)
            {
                string destination = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/IndexFotos");

                //Verifico se existe já esse caminho
                if (!Directory.Exists(destination))
                {
                    //crio
                    Directory.CreateDirectory(destination);
                }


                //Passo por todas as Fotos Recebidas
                foreach (var f in Foto)
                {

                    string extensao = System.IO.Path.GetExtension(f.FileName);

                    // Verifico se está numa das extensões permitidas
                    if (extensao.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                        extensao.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        extensao.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        //yes

                        //Destino Final
                        var finalDestination = Path.Combine(destination, f.FileName);

                        //Crio uma Stream nesse caminho,coloco-o lá e fecho
                        using (FileStream fs = new FileStream(finalDestination, FileMode.Create))
                        {
                            f.CopyTo(fs);

                        }

                        statusMessages.Add($"A foto {f.FileName} foi submetida com sucesso");
                    }
                    else
                    {

                        errorsMessages.Add($"A foto submetida {f.FileName} não é do tipo permitida png, jpeg, jpg");

                    }



                }

            }

            TempData["statusMessages"] = statusMessages;
            TempData["errorsMessages"] = errorsMessages;

            return RedirectToAction(nameof(ControlIndex));



        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApagarFoto(string name)
        {
            string folderFotos = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/IndexFotos");
            List<string> errorsMessages = new List<string>();
            List<string> statusMessages = new List<string>();


            // Verifico se a a pasta existe
            if (Directory.Exists(folderFotos))
            {
                string fotoPath = Path.Combine(folderFotos, name);
                if (System.IO.File.Exists(fotoPath))
                {
                    System.IO.File.Delete(fotoPath);
                    statusMessages.Add($"A foto {name} foi removida com sucesso");
                }
                else
                {
                    errorsMessages.Add($"Houve um imprevisto!A fotografia {name} não foi encontrada :<");

                }
            }
            else
            {
                errorsMessages.Add("Houve um imprevisto!A pasta não foi encontrada :<");
            }


            TempData["statusMessages"] = statusMessages;
            TempData["errorsMessages"] = errorsMessages;
            return RedirectToAction(nameof(ControlIndex));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ControlIndex()
        {
            ViewBag.statusMessages = TempData.TryGetValue("statusMessages", out var statusMessages) ? statusMessages : null;
            ViewBag.errorsMessages = TempData.TryGetValue("errorsMessages", out var errorMessages) ? errorMessages : null;

            IndexJsonModel indexJsonModel = new IndexJsonModel();
            IndexEditModel indexEditModel = new IndexEditModel();

            string folderFotos = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/IndexFotos");

            indexJsonModel = await GetIndexJson();

            indexEditModel.HtmlText = indexJsonModel.HtmlText;

            // Verifico se a a pasta existe
            if (Directory.Exists(folderFotos))
            {
                // Get dos nomes da pasta
                string[] fotoNames = Directory.GetFiles(folderFotos)
                                               .Select(Path.GetFileName)
                                               .ToArray();


                foreach (var fotoName in fotoNames)
                {

                    indexEditModel.IndexFotos.Add(fotoName);
                }
            }
            else
            {
                Directory.CreateDirectory(folderFotos);
                //Crio o directorio mas fica vazio
            }



            return View(indexEditModel);
        }

        [HttpGet]
        public IActionResult Search(string search)
        {
            List<Roupa> searchedRoupas = new List<Roupa>();
            List<Stock> stock = new List<Stock>();

            if (!String.IsNullOrWhiteSpace(search))
            {
                searchedRoupas = _context.Roupa.Include(x => x.Categoria).Where(x => x.Name.Contains(search)).ToList();


                stock = _context.StockMaterial.Include(r => r.Roupa).Where(x => x.Roupa.Name.Contains(search)).ToList();


            }
            else
            {
                searchedRoupas = _context.Roupa.Include(x => x.Categoria).ToList();
                stock = _context.StockMaterial.Include(r => r.Roupa).ToList();
            }


            foreach (var roupa in searchedRoupas)
            {
                foreach (var s in stock)
                {
                    if (roupa.Id == s.RoupaId)
                    {
                        roupa.Quantidade += s.Quantidade;
                    }
                }
            }

            return PartialView("ListingIndex", searchedRoupas);
        }

        [HttpGet]
        public IActionResult RemoverFiltro()
        {
            var allRoupas = _context.Roupa.Include(x => x.Categoria).ToList();


            var stock = _context.StockMaterial.Include(r => r.Roupa).ToList();

            foreach (var roupa in allRoupas)
            {
                foreach (var s in stock)
                {
                    if (roupa.Id == s.RoupaId)
                    {
                        roupa.Quantidade += s.Quantidade;
                    }
                }
            }



            return PartialView("ListingIndex", allRoupas);
        }

        [HttpGet]
        public IActionResult AplicarFiltro(string search,string categories, string sizes)
        {

            List<string> categorias = categories == null ? new List<string>() : categories.Split(",").ToList();
            List<string> tamanhos = sizes == null ? new List<string>() : sizes.Split(",").ToList();

            var allRoupas = _context.Roupa.Include(x => x.Categoria).ToList();

            List<Roupa> selectedCategoriasRoupas = new List<Roupa>();
            List<Roupa> allSelected = new List<Roupa>();
            List<Roupa> trueSelected = new List<Roupa>();

            if (!String.IsNullOrWhiteSpace(search))
            {
                if (allRoupas != null)
                {
                    if (categorias.Count == 0)
                    {
                        selectedCategoriasRoupas = allRoupas.Where(x => x.Name.Contains(search)).ToList();
                    }
                    else
                    {
                        selectedCategoriasRoupas = allRoupas.Where(r => categorias.Contains(r.Categoria.Tipo) && r.Name.Contains(search)).ToList();
                    }
                }
            }
            else
            {
                if (allRoupas != null)
                {
                    if (categorias.Count == 0)
                    {
                        selectedCategoriasRoupas = allRoupas;
                    }
                    else
                    {
                        selectedCategoriasRoupas = allRoupas.Where(r => categorias.Contains(r.Categoria.Tipo)).ToList();
                    }
                }

               
            }


            var stock = _context.StockMaterial.Include(r => r.Roupa).Where(x => selectedCategoriasRoupas.Contains(x.Roupa)).ToList();
            var stockSelected = new List<Stock>();

            if (!String.IsNullOrWhiteSpace(search))
            {
                if (tamanhos.Count == 0)
                {
                    stockSelected = stock.Where(x=>x.Roupa.Name.Contains(search)).ToList();
                }
                else
                {
                    stockSelected = stock.Where(x => tamanhos.Contains(x.Tamanho) && x.Roupa.Name.Contains(search)).ToList();

                }
            }
            else
            {
                if (tamanhos.Count == 0)
                {
                    stockSelected = stock;
                }
                else
                {
                    stockSelected = stock.Where(x => tamanhos.Contains(x.Tamanho)).ToList();

                }


            }
           

            foreach (var s in stockSelected)
            {
                allSelected.Add(s.Roupa);
            }

            trueSelected = allSelected.Distinct().ToList();

            foreach (var r in trueSelected)
            {
                foreach (var s in stockSelected)
                {
                    if (s.RoupaId == r.Id)
                    {
                        r.Quantidade += s.Quantidade;
                    }
                }
            }



            return PartialView("ListingIndex", trueSelected);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> UpdateIndexModelHtml(IFormCollection formData)
        {
            IndexJsonModel indexJsonModel = new IndexJsonModel()
            {
                HtmlText = formData["Htmltext"].ToString()
            };

            await SaveIndexJson(indexJsonModel);

            return RedirectToAction(nameof(ControlIndex));
        }



        public async Task SaveIndexJson(IndexJsonModel indexJsonModel)
        {
            string jsonDataFolder = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/IndexJson");

            indexJsonModel.HtmlText = HttpUtility.HtmlEncode(indexJsonModel.HtmlText);

            // Verifico se a pasta existe
            if (Directory.Exists(jsonDataFolder))
            {
                //A pasta existe topzão

                string jsonDataPath = Path.Combine(jsonDataFolder, "index.json");


                //Não Existe
                try
                {
                    //Cria o ficheiro
                    //Não é preciso fechar a Stream pq no fim do Scope do Using é fechada automaticamente
                    using (FileStream createStream = new FileStream(jsonDataPath, FileMode.Create, FileAccess.Write))
                    {

                        // Torno em Json
                        string jsonData = JsonSerializer.Serialize(indexJsonModel, new JsonSerializerOptions { WriteIndented = true });

                        // Vejo o tamnho do ficheiro e escrevo o ficheiro
                        byte[] jsonDataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                        await createStream.WriteAsync(jsonDataBytes, 0, jsonDataBytes.Length);
                    }

                }
                catch (Exception ex)
                {
                    await ErrorHandlingForSavingError(jsonDataPath, indexJsonModel);
                    Console.WriteLine($"Erro a criar o ficheiro: {ex.Message}");
                }


            }
            else
            {
                //Não Existe a PASTA AAAA
                Directory.CreateDirectory(jsonDataFolder);
                string jsonDataPath = Path.Combine(jsonDataFolder, "index.json");
                try
                {
                    //Cria o ficheiro
                    //Não é preciso fechar a Stream pq no fim do Scope do Using é fechada automaticamente
                    using (FileStream createStream = new FileStream(jsonDataPath, FileMode.Create, FileAccess.Write))
                    {

                        // Torno em Json
                        string jsonData = JsonSerializer.Serialize(indexJsonModel, new JsonSerializerOptions { WriteIndented = true });

                        // Vejo o tamnho do ficheiro e escrevo o ficheiro
                        byte[] jsonDataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                        await createStream.WriteAsync(jsonDataBytes, 0, jsonDataBytes.Length);
                    }

                }
                catch (Exception ex)
                {
                    await ErrorHandlingForSavingError(jsonDataPath, indexJsonModel);
                    Console.WriteLine($"Erro a criar o ficheiro: {ex.Message}");
                }
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IndexJsonModel> GetIndexJson()
        {
            IndexJsonModel indexJsonModel = new IndexJsonModel();

            string jsonDataFolder = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/IndexJson");

            // Verifico se a pasta existe
            if (Directory.Exists(jsonDataFolder))
            {
                //A pasta existe topzão

                string jsonDataPath = Path.Combine(jsonDataFolder, "index.json");

                // Verificar se o ficheiro existe
                if (!System.IO.File.Exists(jsonDataPath))
                {
                    //Não Existe
                    try
                    {
                        //Cria o ficheiro
                        //Não é preciso fechar a Stream pq no fim do Scope do Using é fechada automaticamente
                        using (FileStream createStream = new FileStream(jsonDataPath, FileMode.Create, FileAccess.Write))
                        {

                            // Torno em Json
                            string jsonData = JsonSerializer.Serialize(indexJsonModel, new JsonSerializerOptions { WriteIndented = true });

                            // Vejo o tamnho do ficheiro e escrevo o ficheiro
                            byte[] jsonDataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                            await createStream.WriteAsync(jsonDataBytes, 0, jsonDataBytes.Length);
                        }

                    }
                    catch (Exception ex)
                    {
                        await ErrorHandlingForSavingError(jsonDataPath, indexJsonModel);
                        Console.WriteLine($"Erro a criar o ficheiro: {ex.Message}");
                    }
                }
                else
                {
                    // Existe
                    try
                    {
                        //Abro o ficheiro
                        using (FileStream readStream = new FileStream(jsonDataPath, FileMode.Open, FileAccess.Read))
                        {

                            indexJsonModel = await JsonSerializer.DeserializeAsync<IndexJsonModel>(readStream);

                        }


                    }
                    catch (Exception ex)
                    {
                        await ErrorHandlingForSavingError(jsonDataPath, indexJsonModel);
                        Console.WriteLine($"Erro a ler o ficheiro: {ex.Message}");

                    }
                }
            }
            else
            {
                //Não Existe a PASTA AAAA
                Directory.CreateDirectory(jsonDataFolder);
                string jsonDataPath = Path.Combine(jsonDataFolder, "index.json");
                try
                {
                    //Cria o ficheiro
                    //Não é preciso fechar a Stream pq no fim do Scope do Using é fechada automaticamente
                    using (FileStream createStream = new FileStream(jsonDataPath, FileMode.Create, FileAccess.Write))
                    {

                        // Torno em Json
                        string jsonData = JsonSerializer.Serialize(indexJsonModel, new JsonSerializerOptions { WriteIndented = true });

                        // Vejo o tamnho do ficheiro e escrevo o ficheiro
                        byte[] jsonDataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                        await createStream.WriteAsync(jsonDataBytes, 0, jsonDataBytes.Length);
                    }

                }
                catch (Exception ex)
                {
                    await ErrorHandlingForSavingError(jsonDataPath, indexJsonModel);
                    Console.WriteLine($"Erro a criar o ficheiro: {ex.Message}");
                }
            }

            indexJsonModel.HtmlText = HttpUtility.HtmlDecode(indexJsonModel.HtmlText);

            return indexJsonModel;
        }


        public async Task ErrorHandlingForSavingError(string jsonDataPath, IndexJsonModel indexJsonModel)
        {
            System.IO.File.Delete(jsonDataPath);
            await SaveIndexJson(indexJsonModel);
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel indexViewModel = new IndexViewModel();
            string folder = Path.Combine(_he.ContentRootPath, "wwwroot/Documentos/IndexFotos");

            IndexJsonModel indexJsonModel = await GetIndexJson();

            indexViewModel.HtmlText = indexJsonModel.HtmlText;

            // Verifico se a apasta existe
            if (Directory.Exists(folder))
            {
                // Get dos nomes da pasta
                string[] fotoNames = Directory.GetFiles(folder)
                                               .Select(Path.GetFileName)
                                               .ToArray();


                foreach (var fotoName in fotoNames)
                {

                    indexViewModel.IndexFotos.Add(fotoName);
                }
            }

            indexViewModel.Roupas = _context.Roupa.Include(x => x.Categoria).ToList();


            var stock = await _context.StockMaterial.Include(r => r.Roupa)
                    .OrderBy(s => s.Roupa.CategoriaId)
                    .ToArrayAsync();

            foreach (var s in stock)
            {
                foreach (var r in indexViewModel.Roupas)
                {
                    if (s.RoupaId == r.Id)
                    {
                        r.Quantidade += s.Quantidade;
                    }
                }
            }



            return View(indexViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IFormCollection formData)
        {

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}