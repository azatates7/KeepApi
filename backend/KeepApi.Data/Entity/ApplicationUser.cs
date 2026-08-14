using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeepApi.Data.Entity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        [Column(TypeName = "NUMBER(1)")]
        public bool IsDeleted { get; set; }
        public int Status {  get; set; }
        /// <summary>Kullanıcının arayüz/özet dili: "tr" | "en".</summary>
        public string PreferredLanguage { get; set; } = "tr";
        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}
