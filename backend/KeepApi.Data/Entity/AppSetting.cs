using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace KeepApi.Data.Entity
{
    [Table("AppSettings")]
    public class AppSetting
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = null!;          // "Jwt:Key", "Smtp:Password" gibi hiyerarşik
        public string Value { get; set; } = null!;          // encrypted ise şifreli metin, değilse düz değer
        [Column(TypeName = "NUMBER(1)")]
        public bool IsEncrypted { get; set; }
        public string? Description { get; set; }
        public string TargetProject { get; set; } = null!; // "KeepApi", "KeepApi.Data" vb.
    }
}