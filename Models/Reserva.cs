﻿using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using ShoraWebsite.Models;
using System.ComponentModel.DataAnnotations;

namespace ShoraWebsite.Models
{


    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        public int RoupaId { get; set; }

        public Roupa? Roupa { get; set; }

        public int PerfilId { get; set; }

        public Perfil? Perfil { get; set; }

        public int Quantidade { get; set; }

        public string? Tamanho { get; set; } 

        public bool Vendida { get; set; } = false;

        public MetodoDeEnvio Envio { get; set; } = MetodoDeEnvio.Nenhum;

    }
}
