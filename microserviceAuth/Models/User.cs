namespace microserviceAuth.Models
{
    using Microsoft.AspNetCore.Identity;
    using System;

    public class User : IdentityUser
    {
        public required string FirstName { get; set; } // Nombre
        public required string LastName { get; set; }  // Apellido
        public required DateTime DateOfBirth { get; set; } // Fecha de Nacimiento (dd/mm/yyyy)

        public string? SessionToken { get; set; } 
    }
}
