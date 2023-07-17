﻿using Humanizer.Localisation.TimeToClockNotation;
using System.ComponentModel.DataAnnotations;

namespace shora.Models
{
    public class Roupa
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "É obrigatorio a existencia de um nome")]
        public string Name { get; set; } = " ";


        [Required(ErrorMessage = "É Obrigatorio ter uma categoria associadada")]
        public int CategoriaId { get; set; }


        public Categoria? Categoria { get; set; }



        [RegularExpression(@"^.+\.([jJ][pP][gG]|[pP][nN][gG])$", ErrorMessage = "Só jpg e png files")]
        public string? Foto { get; set; }



    }
}