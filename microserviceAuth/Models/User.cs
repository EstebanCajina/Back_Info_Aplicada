namespace microserviceAuth.Models
{
    using Microsoft.AspNetCore.Identity;

    public class User : IdentityUser
    {
        public string FirstName { get; set; } // Nombre
        public string LastName { get; set; }  // Apellido
        public DateTime DateOfBirth { get; set; } // Fecha de Nacimiento (dd/mm/yyyy)
    }
}
