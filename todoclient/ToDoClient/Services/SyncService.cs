using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ORM;
using ToDoClient.Models;
using System.Data.Entity;
using ToDoClient.Services;

namespace todoclient.Services
{
    public class SyncService
    {
        static Queue<Task> delQueue;
        static Queue<Task> addQueue;
        static ToDoService remoteService;
        static QueueDB db;

        static SyncService()
        {
            remoteService = new ToDoService();
            db = new QueueDB();
            delQueue = new Queue<Task>(db.Tasks.Where(t => t.IsDeleted));
            addQueue = new Queue<Task>(db.Tasks.Where(t => !t.IsUploaded && !t.IsDeleted));
        }

        static public void AddToDelQueue(Task task)
        {
            if (addQueue.Contains(task))
            {
                addQueue = new Queue<Task>(db.Tasks.Where(t => !t.IsUploaded && !t.IsDeleted));
            }
            else
            {
                delQueue.Enqueue(task);
            }
        }

        static public void AddToAddQueue(Task task)
        {
            addQueue.Enqueue(task);
        }

        static public void SyncStart()
        {
            while (true)
            {
                if (addQueue.Count != 0) RemoteAdd(addQueue.Dequeue());
                if (delQueue.Count != 0) RemoteDel(delQueue.Dequeue());
            }
        }


        static private void RemoteAdd(Task task)
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

        static private void RemoteDel(Task task)
        {
            remoteService.DeleteItem(task.RemoteTaskId);
            DeleteFromDB(task.Id);
        }

        static private void RemoteUpdate(Task task)
        {
            remoteService.UpdateItem(new ToDoItemViewModel
            {
                IsCompleted = task.IsCompleted,
                Name = task.Name,
                ToDoId = task.RemoteTaskId,
                UserId = task.RemoteUserId
            });
        }

        private static void Update(Task item)
        {
            var dbTask = db.Set<Task>().Single(u => u.Id == item.Id);
            db.Entry(dbTask).CurrentValues.SetValues(item);
            db.Entry(dbTask).State = EntityState.Modified;
            db.SaveChanges();
        }


        public static void DeleteFromDB(int id)
        {
            var dbTask = db.Set<Task>().Single(u => u.Id == id);
            db.Tasks.Remove(dbTask);
            db.SaveChanges();
        }


    }
}