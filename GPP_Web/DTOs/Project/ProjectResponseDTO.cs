﻿
using GPP_API.DTO.User;
using GPP_Web.DTOs.Alert;
using GPP_Web.DTOs.BudgetPart;
using GPP_Web.DTOs.User;

namespace GPP_Web.DTOs.Project
{
    public class ProjectResponseDTO
    {
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public decimal RemainingBudget { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }

        public string? ManagerEmail { get; set; }
        public UserResponseDTO? Manager { get; set; }

        public List<AlertDTO> Alerts { get; set; } = new();
        public List<BudgetPartResponseDTO> BudgetParts { get; set; } = new();

    }

   
}
