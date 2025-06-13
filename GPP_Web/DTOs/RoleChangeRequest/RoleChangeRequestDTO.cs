﻿namespace GPP_Web.DTOs.RoleChangeRequest
{
    public class RoleChangeRequestDTO
    {
        public int RequestId { get; set; }
        public string? RequestedRole { get; set; }
        public string? Justification { get; set; }
        public string? EmailAddress { get; set; } 
        public string? FullName { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}
