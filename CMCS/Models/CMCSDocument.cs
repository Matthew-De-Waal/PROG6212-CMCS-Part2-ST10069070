﻿using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class CMCSDocument
    {
        // Automatic Properties
        [Key]
        public int DocumentID { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long Size { get; set; }
        public string? Section { get; set; }
        public string? Content { get; set; }
    }
}
