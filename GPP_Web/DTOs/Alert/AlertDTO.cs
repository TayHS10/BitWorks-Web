﻿namespace GPP_Web.DTOs.Alert
{
    public class AlertDTO
    {
        public int AlertId { get; set; }
        public string? AlertType { get; set; }
        public string Message { get; set; } = null!;
        public DateTime? AlertDate { get; set; }
    }

}
