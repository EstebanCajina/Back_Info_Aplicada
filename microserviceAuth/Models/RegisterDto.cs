namespace microserviceAuth.Models
{
    public class RegisterDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FirstName { get; set; } // Nombre
        public required string LastName { get; set; }  // Apellido
        public required string DateOfBirth { get; set; } // Fecha de Nacimiento (dd/mm/yyyy)
    }

}
