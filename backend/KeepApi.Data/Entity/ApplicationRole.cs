using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeepApi.Data.Entity
{
    public class ApplicationRole : IdentityRole<Guid>
    {
    }
}
