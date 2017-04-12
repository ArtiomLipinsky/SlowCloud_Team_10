using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM
{
    public class ToDoItem
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RemoteUserId { get; set; }
        public int RemoteTaskId { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsChanged { get; set; }
        public bool IsUploaded { get; set; }

        public User User { get; set; }

        public override bool Equals(object obj)
        {
            ToDoItem task = (ToDoItem)obj;
            if (Name == task.Name) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return UserId*5+RemoteTaskId*3-RemoteUserId*2; //!!!!
        }

    }
}
