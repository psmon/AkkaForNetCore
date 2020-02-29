using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkaNetCore.Models.Entity
{
    [Table("tbl_message_reseved")]
    public class MessageReseved
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int no { get; set; }

        [Column("seq")]
        public string Seq { get; set; }


        [Column("message")]
        public string Message { get; set; }

        [Column("update_time")]
        public DateTime updateTime { get; set; }
    }
}
