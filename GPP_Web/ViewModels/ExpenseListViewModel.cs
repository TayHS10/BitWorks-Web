using GPP_Web.DTOs.Expense;

namespace GPP_Web.ViewModels
{
    public class ExpenseListViewModel
    {
        public List<ExpenseResponseDTO> Expenses { get; set; } = new List<ExpenseResponseDTO>();
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string InfoMessage { get; set; } = string.Empty;

        public int BudgetPartId { get; set; } // ID de la partida actual
        public int ProjectId { get; set; }   // ID del proyecto al que pertenece la partida, para el enlace de "volver"
    }
}
