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
    /// <summary>
    /// Works with Users backend.
    /// </summary>
    public class LocalUserService
    {
        QueueDB db;
        UserService lowService;

        public LocalUserService()
        {
            db = new QueueDB();
           lowService = new UserService();
        }

    
        public int CreateUser(string userName)
        {
            int id= lowService.CreateUser(userName);
            CreateUser(userName, id);
            return id;
        }


        public int CreateUser(string userName, int Id)
        {
            db.Users.Add(new User { Name = userName, RemoteId= Id });
            db.SaveChanges();
            return db.Users.Single(u => u.Name == userName).RemoteId;
        }

        public int GetOrCreateUser()
        {
            var userCookie = HttpContext.Current.Request.Cookies["user"];
            int userId;
            string userName = "Noname: " + Guid.NewGuid();


            // No user cookie or it's damaged
            if (userCookie == null || !Int32.TryParse(userCookie.Value, out userId))
            {
              
                userId = CreateUser(userName);

                // Store the user in a cookie for later access
                var cookie = new HttpCookie("user", userId.ToString())
                {
                    Expires = DateTime.Today.AddMonths(1)
                };

                HttpContext.Current.Response.SetCookie(cookie);
            }


            if (!db.Users.Any(u=>u.RemoteId== userId))
            {
                CreateUser(userName, userId);
            }

            return userId;
        }
    }
}