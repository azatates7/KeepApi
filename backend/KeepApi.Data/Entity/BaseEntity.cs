using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace KeepApi.Data.Entity
{
    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int Status { get; set; }

        [Column(TypeName = "NUMBER(1)")]
        public bool IsDeleted { get; set; }
    }
}
