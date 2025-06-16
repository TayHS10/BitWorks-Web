﻿using GPP_Web.DTOs.Project;

namespace GPP_Web.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public int? StatusCode { get; set; }
    }
}
