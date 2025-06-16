﻿using System.ComponentModel.DataAnnotations;

namespace GPP_Web.DTOs.Expense
{
    public class CreateExpenseDTO
    {
        [Required(ErrorMessage = "El ID de la partida presupuestaria es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la partida presupuestaria debe ser un número positivo.")]
        public int BudgetPartId { get; set; } // Enlaza el gasto a una partida

        [Required(ErrorMessage = "El monto del gasto es obligatorio.")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "El monto del gasto debe ser mayor que cero.")]
        public decimal ExpenseAmount { get; set; }

        [Required(ErrorMessage = "La fecha del gasto es obligatoria.")]
        public DateOnly ExpenseDate { get; set; }

        [StringLength(255, ErrorMessage = "La referencia del documento no puede exceder los 255 caracteres.")]
        public string? DocumentReference { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
        public string? Description { get; set; }

        // Campo para recibir el archivo (imagen)
        public IFormFile? DocumentFile { get; set; }  // Para cargar el archivo
    }
}
