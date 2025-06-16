
using GPP_Web.DTOs.Expense;

namespace GPP_Web.DTOs.BudgetPart
{
    public class BudgetPartResponseDTO
    {
        public int BudgetPartId { get; set; }
        public int ProjectId { get; set; }
        public string PartName { get; set; } = null!;
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<ExpenseResponseDTO> Expenses { get; set; } = new();
    }

}
