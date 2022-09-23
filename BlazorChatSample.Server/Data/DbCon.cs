using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorChatSample.Server.Data
{
    public class DbCon: DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbCon(DbContextOptions<DbCon> options):base(options)
        {
        }
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Name{ get; set; }
        public string Password { get; set; }
    }

    public class Message
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public Guid From { get; set; }
        public Guid ChatId { get; set; }
    }
}
