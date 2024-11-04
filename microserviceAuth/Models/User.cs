namespace microserviceAuth.Models
{
    using Microsoft.AspNetCore.Identity;
    using System;

    public class User : IdentityUser
    {
        public string FirstName { get; set; } // Nombre
        public string LastName { get; set; }  // Apellido
        public DateTime DateOfBirth { get; set; } // Fecha de Nacimiento (dd/mm/yyyy)

        public string? SessionToken { get; set; } 
    }
}
