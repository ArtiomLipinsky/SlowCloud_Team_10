
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ORM;
using ToDoClient.Models;
using System.Data.Entity;
using ToDoClient.Services;
using System.Threading.Tasks;

namespace todoclient.Services
{
    public class SyncService
    {
        static Queue<ToDoItem> delQueue;
        static Queue<ToDoItem> addQueue;
        static Queue<ToDoItem> updateQueue;
        static ToDoService remoteService;
        static QueueDB db;

        static SyncService()
        {
            remoteService = new ToDoService();
            db = new QueueDB();
            delQueue = new Queue<ToDoItem>(db.Tasks.Where(t => t.IsDeleted));
            addQueue = new Queue<ToDoItem>(db.Tasks.Where(t => !t.IsUploaded && !t.IsDeleted));
            updateQueue = new Queue<ToDoItem>(db.Tasks.Where(t => t.IsChanged));
        }

        static public void AddToDelQueue(ToDoItem task)
        {
            if (addQueue.Contains(task))
            {
                addQueue = new Queue<ToDoItem>(db.Tasks.Where(t => !t.IsUploaded && !t.IsDeleted));
            }
            if (updateQueue.Contains(task))
            {
                updateQueue = new Queue<ToDoItem>(db.Tasks.Where(t => t.IsChanged && !t.IsDeleted));
            }
            delQueue.Enqueue(task);

        }

        static public void AddToAddQueue(ToDoItem task)
        {

            addQueue.Enqueue(task);
        }

        static public void AddToUpdateQueue(ToDoItem task)
        {
            updateQueue.Enqueue(task);
        }

        static public void SyncStart()
        {
            while (true)
            {
                if (addQueue.Count != 0) RemoteAdd(addQueue.Dequeue());
                if (updateQueue.Count != 0) RemoteUpdate(updateQueue.Dequeue());
                if (delQueue.Count != 0) RemoteDel(delQueue.Dequeue());
            }
        }

        static private void RemoteAdd(ToDoItem task)
        {
            remoteService.CreateItem(new ToDoItemViewModel
            {
                IsCompleted = task.IsCompleted,
                Name = task.Name,
                ToDoId = task.RemoteTaskId,
                UserId = task.RemoteUserId
            });
            task.IsUploaded = true;
            Update(task);
        }

        static public void SynchronizationStart()
        {
            Task syncTask = Task.Factory.StartNew(Start);
        }

        static private void Start()
        {

            while (true)
            {
                Refresh();
                if (addQueue.Count != 0) RemoteAdd(addQueue.Dequeue());
                if (updateQueue.Count != 0) RemoteUpdate(updateQueue.Dequeue());
                if (delQueue.Count != 0) RemoteDel(delQueue.Dequeue());
            }

        }

        static private void RemoteDel(ToDoItem task)
        {
            if (!task.IsUploaded)
            {
                DeleteFromDB(task.Id);
            }
            else
            {
                if (task.RemoteTaskId == 0) Refresh();
                remoteService.DeleteItem(task.RemoteTaskId);
                DeleteFromDB(task.Id);
            }
        }

        static private void RemoteUpdate(ToDoItem task)
        {
            if (!task.IsUploaded)
            {
                Update(task);
            }
            else
            {
                if (task.RemoteTaskId == 0) Refresh();
                remoteService.UpdateItem(new ToDoItemViewModel
                {
                    IsCompleted = task.IsCompleted,
                    Name = task.Name,
                    ToDoId = task.RemoteTaskId,
                    UserId = task.RemoteUserId
                });
                task.IsChanged = false;
                Update(task);
            }
        }

        private static void Update(ToDoItem item)
        {
            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == item.Id);
            db.Entry(dbTask).CurrentValues.SetValues(item);
            db.Entry(dbTask).State = EntityState.Modified;
            db.SaveChanges();
        }

        public static void DeleteFromDB(int id)
        {
            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == id);
            db.Tasks.Remove(dbTask);
            db.SaveChanges();
        }

        private static void Refresh()
        {
            var taskWithoutRemoteID = db.Tasks.Where(t => t.RemoteTaskId == 0).ToList();

            if (taskWithoutRemoteID.Count > 0)
            {
                foreach (int userId in taskWithoutRemoteID.Select(t => t.RemoteUserId).Distinct())
                {
                    if (userId != 0)
                    {
                        IList<ToDoItem> tasks = remoteService.GetItems(userId).Select(t => new ToDoItem
                        {
                            Name = t.Name,
                            RemoteUserId = t.UserId,
                            RemoteTaskId = t.ToDoId,

                        }).ToList();

                        foreach (var task in tasks)
                        {
                            //var temp = db.Tasks.FirstOrDefault(t => t.Name == task.Name && t.RemoteTaskId == 0);
                            var temp = db.Tasks.FirstOrDefault(t => t.Name == task.Name);
                            if (temp != null)
                            {
                                temp.RemoteTaskId = task.RemoteTaskId;
                                Update(temp);
                            }
                        }
                    }
                }

            }
        }




    }
}



//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using ORM;
//using ToDoClient.Models;
//using System.Data.Entity;
//using ToDoClient.Services;
//using System.Threading.Tasks;
//using System.Threading;

//namespace todoclient.Services
//{
//    public class SyncService
//    {
//        static Queue<ToDoItem> delQueue;
//        static Queue<ToDoItem> addQueue;
//        static Queue<ToDoItem> updateQueue;
//        static ToDoService remoteService;
//        static QueueDB db;

//        static SyncService()
//        {
//            remoteService = new ToDoService();
//            db = new QueueDB();
//            delQueue = new Queue<ToDoItem>(db.Tasks.Where(t => t.IsDeleted));
//            addQueue = new Queue<ToDoItem>(db.Tasks.Where(t => !t.IsUploaded && !t.IsDeleted));
//            updateQueue = new Queue<ToDoItem>(db.Tasks.Where(t => t.IsChanged));
//        }

//        static public void AddToDelQueue(ToDoItem task)
//        {
//            if (task == null) throw new ArgumentNullException(nameof(task));
//            //if (task.RemoteTaskId == 0) Refresh();

//            if (addQueue.Contains(task))
//            {
//                addQueue = new Queue<ToDoItem>(db.Tasks.Where(t => !t.IsUploaded && !t.IsDeleted));
//            }
//            if (updateQueue.Contains(task))
//            {
//                updateQueue = new Queue<ToDoItem>(db.Tasks.Where(t => t.IsChanged && !t.IsDeleted));
//            }
//            delQueue.Enqueue(task);
//        }

//        static public void AddToAddQueue(ToDoItem task)
//        {
//            if (task == null) throw new ArgumentNullException(nameof(task));
//            addQueue.Enqueue(task);
//        }

//        static public void AddToUpdateQueue(ToDoItem task)
//        {
//            if (task == null) throw new ArgumentNullException(nameof(task));
//            //if (task.RemoteTaskId == 0) Refresh();

//            updateQueue.Enqueue(task);
//        }

//        static public void SynchronizationStart()
//        {
//            Task syncTask = Task.Factory.StartNew(Start);
//        }

//        static private void Start()
//        {

//            while (true)
//            {
//                Refresh();
//                if (addQueue.Count != 0) RemoteAdd(addQueue.Dequeue());
//                if (updateQueue.Count != 0) RemoteUpdate(updateQueue.Dequeue());
//                if (delQueue.Count != 0) RemoteDel(delQueue.Dequeue());
//            }

//        }


//        private static void RemoteAdd(ToDoItem task)
//        {
//            remoteService.CreateItem(new ToDoItemViewModel
//            {
//                Id = task.Id,
//                IsCompleted = task.IsCompleted,
//                Name = task.Name,
//                ToDoId = task.RemoteTaskId,
//                UserId = task.RemoteUserId
//            });
//            task.IsUploaded = true;
//            Update(task);
//        }

//        private static void RemoteDel(ToDoItem task)
//        {
//            remoteService.DeleteItem(task.RemoteTaskId);
//            DeleteFromDB(task.Id);
//        }

//        private static void RemoteUpdate(ToDoItem task)
//        {
//            remoteService.UpdateItem(new ToDoItemViewModel
//            {
//                Id = task.Id,
//                IsCompleted = task.IsCompleted,
//                Name = task.Name,
//                ToDoId = task.RemoteTaskId,
//                UserId = task.RemoteUserId
//            });
//            task.IsChanged = false;
//            Update(task);
//        }

//        private static void Update(ToDoItem item)
//        {
//            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == item.Id);
//            db.Entry(dbTask).CurrentValues.SetValues(item);
//            db.Entry(dbTask).State = EntityState.Modified;
//            db.SaveChanges();
//        }

//        private static void DeleteFromDB(int id)
//        {
//            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == id);
//            db.Tasks.Remove(dbTask);
//            db.SaveChanges();
//        }

//        private static void Refresh()
//        {
//            var taskWithoutRemoteID = db.Tasks.Where(t => t.RemoteTaskId == 0).ToList();

//            if (taskWithoutRemoteID.Count > 0)
//            {
//                foreach (int userId in taskWithoutRemoteID.Select(t => t.RemoteUserId).Distinct())
//                {
//                    if (userId != 0)
//                    {
//                        IEnumerable<ToDoItem> tasks = remoteService.GetItems(userId).Select(t => new ToDoItem
//                        {
//                            IsCompleted = t.IsCompleted,
//                            IsUploaded = true,
//                            IsChanged = false,
//                            Name = t.Name,
//                            RemoteUserId = t.UserId,
//                            RemoteTaskId = t.ToDoId,
//                            User = db.Users.Single(u => u.RemoteId == userId)
//                        });

//                        foreach (var task in tasks)
//                        {
//                            Update(task);
//                        }
//                    }
//                }

//            }
//        }


//    }
//}
