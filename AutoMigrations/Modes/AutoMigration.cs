using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMigrations.Modes
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AutoMigration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column(Order = 0)]
        public int Id { get; set; }

        public AutoMigration(byte[]? migrations, string? name, string? key)
        {
            Migrations = migrations;
            Name = name;
            MigrationTime = DateTime.Now;
            Key = key;
        }

        [Column(Order = 3)]
        [Required]
        public byte[]? Migrations { get; set; }

        [Column(Order = 1)]
        [Required]
        [StringLength(128)]
        public string? Name { get; set; }

        [Column(Order = 2)]
        [Required]
        [StringLength(128)]
        public string? Key { get; set; }

        [Column(Order = 3)]
        public DateTime MigrationTime { get; set; }
    }
}
