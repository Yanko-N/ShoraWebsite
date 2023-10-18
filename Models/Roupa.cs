using System.ComponentModel.DataAnnotations;

namespace shora.Models
{
    public class Roupa
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "É obrigatorio a existencia de um nome")]
        public string Name { get; set; } = " ";


        [Required(ErrorMessage = "É obrigatorio ter uma categoria associadada")]
        public int CategoriaId { get; set; }


        public Categoria? Categoria { get; set; }



        [RegularExpression(@"^.+\.([jJ][pP][eE][gG]|[jJ][pP][gG]|[pP][nN][gG])$", ErrorMessage = "Só jpg e png files")]
        public string? Foto { get; set; }


        public int Quantidade { get; set; }

        [Required(ErrorMessage ="É obrigatorio existir um preço")]
        public float Preco { get; set; }


    }
}
