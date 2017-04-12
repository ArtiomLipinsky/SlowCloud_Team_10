using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace ORM
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Validation;
    using System.Diagnostics;
    using System.IO;


    public class QueueDB : DbContext
    {
        // Your context has been configured to use a 'EntityModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'ORM.EntityModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'EntityModel' 
        // connection string in the application configuration file.
        public QueueDB()
            : base("name=QueueDB")
        {


        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<ToDoItem> Tasks { get; set; }


        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Debug.WriteLine("Entity of type {0}| in state {1}| has thefollowing validation errors: ", eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
            catch (DbUpdateException e)
            {
                //Add your code to inspect the inner exception and/or
                //e.Entries here.
                //Or just use the debugger.
                //Added this catch (after the comments below) to make it more obvious 
                //how this code might help this specific problem

                Debug.WriteLine(e.Message);
                throw;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

 
            modelBuilder.Entity<ToDoItem>()
                .HasRequired(p => p.User)
                .WithMany(e => e.Tasks)
                .HasForeignKey(u => u.UserId).
                WillCascadeOnDelete(false);


        }

        new public void Dispose()
        {
            base.Dispose();
        }
    }
}



