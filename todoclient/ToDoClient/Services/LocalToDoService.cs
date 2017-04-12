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
    public class LocalToDoService
    {

        QueueDB db;
        ToDoService remoteService;

        public LocalToDoService()
        {
            db = new QueueDB();
            remoteService = new ToDoService();
        }

        public async Task<IList<ToDoItemViewModel>> GetItemsAsync(int userId)
        {
            return await Task.Run(() =>
                {
                    db.Tasks.AddRange(remoteService.GetItems(userId).Select(t => new ToDoItem
                    {
                        IsCompleted = t.IsCompleted,
                        IsUploaded = true,
                        IsChanged = false,
                        Name = t.Name,
                        RemoteUserId = t.UserId,
                        RemoteTaskId = t.ToDoId,
                        User = db.Users.Single(u => u.RemoteId == userId)
                    }));
                    db.SaveChanges();

                    return db.Set<ToDoItem>().Where(u => u.RemoteUserId == userId && !u.IsDeleted).Select(
                    t =>
                        new ToDoItemViewModel
                        {
                            Id = t.Id,
                            UserId = t.UserId,
                            Name = t.Name,
                            ToDoId = t.RemoteTaskId,
                            IsCompleted = t.IsCompleted
                        }
                    ).ToList();
                }
            );
        }


        public IList<ToDoItemViewModel> GetItems(int userId)
        {
            if (db.Tasks.Where(u => u.RemoteUserId == userId).Count() == 0)
            {
                return GetItemsAsync(userId).Result;
            }
            return db.Set<ToDoItem>().Where(u => u.RemoteUserId == userId && !u.IsDeleted).Select(
                t =>
                    new ToDoItemViewModel
                    {
                        Id = t.Id,
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
            var temp = new ToDoItem
            {
                IsCompleted = false,
                IsUploaded = false,
                IsChanged = false,
                Name = item.Name,
                User = user,
                RemoteUserId = item.UserId,
                RemoteTaskId = item.ToDoId
            };
            var tempId = AddAndGetID(temp);
            db.SaveChanges();
            SyncService.AddToAddQueue(db.Tasks.Single(t => t.Id == tempId));
        }

        /// <summary>
        /// !!!!!!!!!!!
        /// </summary>
        /// <param name="item"></param>
        public void UpdateItem(ToDoItemViewModel item)
        {
            var temp = db.Tasks.Single(t => t.Id == item.Id);
            temp.IsCompleted = item.IsCompleted;
            temp.IsChanged = true;
            Update(temp);
            SyncService.AddToUpdateQueue(temp);
        }

        public void DeleteItem(int id)
        {
            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == id);
            dbTask.IsDeleted = true;
            Update(dbTask);
            db.SaveChanges();
            SyncService.AddToDelQueue(db.Tasks.Single(t => t.Id == id));
        }

        private void Update(ToDoItem item)
        {
            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == item.Id);
            db.Entry(dbTask).CurrentValues.SetValues(item);
            db.Entry(dbTask).State = EntityState.Modified;
            db.SaveChanges();
            SyncService.AddToUpdateQueue(db.Tasks.Single(t => t.Id == item.Id));
        }

        public void DeleteFromDB(int id)
        {
            var dbTask = db.Set<ToDoItem>().Single(u => u.Id == id);
            db.Tasks.Remove(dbTask);
            db.SaveChanges();
        }

        public int AddAndGetID(ToDoItem task)
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
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }

                return newTaskID;
            }
        }

    }
}