using AutoMigrations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

namespace ConsoleApp1;

public class ProgramDbContext : DbContext
{
    DbContextOptions dbContextOptions;

    public ProgramDbContext(DbContextOptions<ProgramDbContext> options)
        : base(options)
    {
        this.dbContextOptions = options;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var services = new ServiceCollection();

        var assembly = typeof(ProgramDbContext).Assembly;

        services.AddDbContextPool<ProgramDbContext>(
            options =>
                options.UseSqlite(
                    $"Data Source={Path.Combine(Environment.CurrentDirectory, "sqlite.db")}",
                    //                options.UseMySql(
                    //                    "Data Source=127.0.0.1;Port=3306;Database=Program;User ID=root;Password=myroot;Charset=utf8; SslMode=none;Min pool size=1",
                    //#if !NET48
                    //                    ServerVersion.Create(
                    //                        new Version(),
                    //                        Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql
                    //                    ),
                    //#endif

                    b => b.MigrationsAssembly(assembly.GetName().Name)
                ),
            64
        );

        var provider = services.BuildServiceProvider();

        using var context = provider.GetRequiredService<ProgramDbContext>();
        var stop = Stopwatch.StartNew();
        context.Database.EnsureCreated();
        stop.Stop();
        Console.WriteLine($"initialize database :{stop.ElapsedMilliseconds} ms");

        stop.Reset();
        stop.Restart();

        context.AutoMigrate(assembly, context.dbContextOptions);

        stop.Stop();
        Console.WriteLine($"auto migrate :{stop.ElapsedMilliseconds} ms");

        context.TestModels.Add(new TestModel());

        context.SaveChanges();

        Console.ReadKey();
    }

    public virtual DbSet<TestModel2> TestModel2s { get; set; }
    public virtual DbSet<TestModel> TestModels { get; set; }
}

public class TestModel
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime222333 { get; set; } = DateTime.Now;
    public DateTime CreateTime222 { get; set; } = DateTime.Now;
    //public DateTime CreateTime2222333 { get; set; } = DateTime.Now;
    //public DateTime CreateTime2222 { get; set; } = DateTime.Now;
    //public DateTime CreateTime { get; set; } = DateTime.Now;
    //public DateTime CreateTime1 { get; set; } = DateTime.Now;
}

public class TestModel2
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime CreateTime1 { get; set; } = DateTime.Now;
}
