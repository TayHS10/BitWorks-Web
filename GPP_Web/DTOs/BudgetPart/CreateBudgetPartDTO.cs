namespace GPP_Web.DTOs.BudgetPart
{
    public class CreateBudgetPartDTO
    {
        public string PartName { get; set; } = null!;
        public decimal AllocatedAmount { get; set; }
    }
}
