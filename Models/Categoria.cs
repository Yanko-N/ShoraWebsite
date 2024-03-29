﻿using System.ComponentModel.DataAnnotations;

namespace ShoraWebsite.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tipo de Roupa")]
        public string Tipo { get; set; } = " ";
    }
}
