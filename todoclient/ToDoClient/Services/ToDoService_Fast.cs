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
    public class ToDoService_Fast
    {

        QueueDB db;
        ToDoService remoteService;

        public ToDoService_Fast()
        {
            db = new QueueDB();
            remoteService = new ToDoService();
        }


        public IList<ToDoItemViewModel> GetItems(int userId)
        {
            if (db.Tasks.Where(u => u.RemoteUserId == userId).Count() == 0)
            {
                db.Tasks.AddRange(remoteService.GetItems(userId).Select(t => new Task
                {
                    IsCompleted = t.IsCompleted,
                    IsUploaded = true,
                    Name = t.Name,
                    RemoteUserId = t.UserId,
                    RemoteTaskId=t.ToDoId,
                    User = db.Users.Single(u => u.RemoteId == userId)
                }));
                db.SaveChanges();
            }

            //return (new ToDoService()).GetItems(userId);
            return db.Set<Task>().Where(u => u.RemoteUserId == userId && !u.IsDeleted).Select(
                t =>
                    new ToDoItemViewModel
                    {
                        UserId = t.UserId,
                        Name = t.Name,
                        ToDoId = t.RemoteTaskId,
                        IsCompleted = t.IsCompleted
                    }
                ).ToList();
        }


        public void CreateItem(ToDoItemViewModel item)
        {
            var user = db.Users.First(u => u.RemoteId == item.UserId);
            var temp = new Task
            {
                IsCompleted = false,
                IsUploaded = false,
                Name = item.Name,
                User = user,
                RemoteUserId = item.UserId,
                RemoteTaskId = item.ToDoId
            };
            var tempId = InsertTaskAndGetID(temp);
            db.SaveChanges();
            SyncService.AddToAddQueue(db.Tasks.Single(t => t.Id == tempId));
        }

        /// <summary>
        /// !!!!!!!!!!!
        /// </summary>
        /// <param name="item"></param>
        public void UpdateItem(ToDoItemViewModel item)
        {


            var temp = new Task
            {
                Id=db.Tasks.Single(t=>t.RemoteTaskId==item.ToDoId).Id,
                RemoteTaskId = item.ToDoId,
                IsCompleted = item.IsCompleted,
                IsUploaded = false,
                Name = item.Name,
                UserId = item.UserId,
                IsDeleted = false,
                RemoteUserId = item.UserId
            };
            Update(temp);
        }


        public void DeleteItem(int id)
        {
            var dbTask = db.Set<Task>().Single(u => u.RemoteTaskId == id);
            dbTask.IsDeleted = true;
            Update(dbTask);
            db.SaveChanges();
            SyncService.AddToDelQueue(db.Tasks.Single(t => t.RemoteTaskId == id));
        }

        private void Update(Task item)
        {
            var dbTask = db.Set<Task>().Single(u => u.Id == item.Id);
            db.Entry(dbTask).CurrentValues.SetValues(item);
            db.Entry(dbTask).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void DeleteFromDB(int id)
        {
            var dbTask = db.Set<Task>().Single(u => u.Id == id);
            db.Tasks.Remove(dbTask);
            db.SaveChanges();
        }

        public int InsertTaskAndGetID(Task task)
        {
            int newTaskID = 0;
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.Tasks.Add(task);
                    db.SaveChanges();
                    newTaskID = task.Id;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }

                return newTaskID;
            }
        }

    }
}