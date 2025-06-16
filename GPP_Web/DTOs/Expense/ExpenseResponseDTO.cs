﻿namespace GPP_Web.DTOs.Expense
{
    public class ExpenseResponseDTO
    {
        public int ExpenseId { get; set; }
        public decimal ExpenseAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly ExpenseDate { get; set; }
        public string? DocumentReference { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}
