using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Name { get; set; }

        public ICollection<ToDoItem> Tasks { get; set; }

        public override bool Equals(object obj)
        {
            User task = (User)obj;
            if (RemoteId == task.RemoteId) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return Id * 5 + RemoteId * 3;
        }

    }
}
