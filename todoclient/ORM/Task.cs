using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM
{
    public class Task
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RemoteUserId { get; set; }
        public int RemoteTaskId { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsUploaded { get; set; }


        public User User { get; set; }

    }
}
