using Microsoft.EntityFrameworkCore;
using ZENO_API_II.Models;

namespace ZENO_API_II.Data
{
    public class ZenoDbContext : DbContext
    {
        public ZenoDbContext(DbContextOptions<ZenoDbContext> options) : base(options) { }

        public DbSet<UserLocal> Users { get; set; }
        public DbSet<AssistantLocal> Assistants { get; set; }
        public DbSet<ChatThread> Threads { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<OpenAIRunLog> OpenAIRunLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User 1:1 Assistant
            modelBuilder.Entity<UserLocal>()
                .HasOne(u => u.Assistant)
                .WithOne(a => a.User)
                .HasForeignKey<AssistantLocal>(a => a.UserLocalId);

            // User 1:N CreditTransactions
            modelBuilder.Entity<CreditTransaction>()
                .HasOne(ct => ct.User)
                .WithMany(u => u.CreditTransactions)
                .HasForeignKey(ct => ct.UserId);

            // Assistant 1:N Threads
            modelBuilder.Entity<AssistantLocal>()
                .HasMany(a => a.Threads)
                .WithOne(t => t.Assistant)
                .HasForeignKey(t => t.AssistantId);

            // Thread 1:N Messages
            modelBuilder.Entity<ChatThread>()
                .HasMany(t => t.Messages)
                .WithOne(m => m.Thread)
                .HasForeignKey(m => m.ThreadId);

            // OpenAIRunLogs
            modelBuilder.Entity<OpenAIRunLog>()
            .Property(l => l.RunId)
            .IsRequired();

            modelBuilder.Entity<OpenAIRunLog>()
                .Property(l => l.Status)
                .HasMaxLength(50);


        }
    }

}
