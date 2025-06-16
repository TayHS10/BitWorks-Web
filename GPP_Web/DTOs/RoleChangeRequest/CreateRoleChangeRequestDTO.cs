using System.ComponentModel.DataAnnotations;

namespace GPP_Web.DTOs.RoleChangeRequest
{
    public class CreateRoleChangeRequestDTO
    {

        public string RequestedRole { get; set; }

        [StringLength(255, ErrorMessage = "La justificación no puede exceder los 255 caracteres.")]
        public string Justification { get; set; }

        // Estos campos no se validan directamente desde el input del usuario en la vista
        // ya que se llenarán automáticamente desde la identidad del usuario en el controlador.
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
    }
}
