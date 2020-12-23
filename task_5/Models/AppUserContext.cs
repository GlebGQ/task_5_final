using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace task_5.Models
{
    public class AppUserContext : IdentityDbContext<AppUser>
    {
        public AppUserContext(DbContextOptions<AppUserContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
