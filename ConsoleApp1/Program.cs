using AutoMigrations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ConsoleApp1
{
    public class Program : DbContext
    {
        public Program(DbContextOptions<Program> options)
            : base(options) { }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var services = new ServiceCollection();

            var assembly = Assembly.GetExecutingAssembly();

            services.AddDbContextPool<Program>(
                options =>
                    options.UseMySql(
                        "Data Source=127.0.0.1;Port=3306;Database=Program;User ID=root;Password=myroot;Charset=utf8; SslMode=none;Min pool size=1",
#if !NET48
                        ServerVersion.Create(
                            new Version(),
                            Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql
                        ),
#endif

                        b => b.MigrationsAssembly(assembly.GetName().Name)
                    ),
                200
            );

            var provider = services.BuildServiceProvider();

            var context = provider.GetRequiredService<Program>();

            // autoMigrateName: a unique name that corresponds one-to-one to dbcontext
            context.AutoMigrate(assembly, "{8FE1740D-28FB-4555-B74A-D2B7A09E16A0}");

            context.TestModels.Add(new TestModel());

            context.SaveChanges();

            Console.ReadKey();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder.Model.FindEntityType(typeof(TestModel)) is null)
            {
                modelBuilder.Model.AddEntityType(typeof(TestModel));
            }

            base.OnModelCreating(modelBuilder);
        }

        public virtual DbSet<TestModel2> TestModel2s { get; set; }
        public virtual DbSet<TestModel> TestModels { get; set; }
    }

    public class TestModel
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime CreateTime1 { get; set; } = DateTime.Now;
    }

    public class TestModel2
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime CreateTime1 { get; set; } = DateTime.Now;
    }
}
