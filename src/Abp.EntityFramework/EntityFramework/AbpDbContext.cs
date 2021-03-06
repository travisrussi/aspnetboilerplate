﻿using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Runtime.Session;

namespace Abp.EntityFramework
{
    public abstract class AbpDbContext : DbContext, ITransientDependency
    {
        public IAbpSession AbpSession { get; set; }

        protected AbpDbContext()
        {
            AbpSession = NullAbpSession.Instance;
        }

        protected AbpDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            AbpSession = NullAbpSession.Instance;
        }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Types<ISoftDelete>().Configure(c => c.HasTableAnnotation(AbpEfConsts.SoftDeleteCustomAnnotationName, true));
        }

        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        SetCreationAuditProperties(entry);
                        break;
                    case EntityState.Modified:
                        PreventSettingCreationAuditProperties(entry);
                        SetModificationAuditProperties(entry);
                        break;
                    case EntityState.Deleted:
                        PreventSettingCreationAuditProperties(entry);
                        HandleSoftDelete(entry);
                        break;
                }
            }

            return base.SaveChanges();
        }

        private void SetCreationAuditProperties(DbEntityEntry entry)
        {
            if (entry.Entity is IHasCreationTime)
            {
                entry.Cast<IHasCreationTime>().Entity.CreationTime = DateTime.Now; //TODO: UtcNow?
            }

            if (entry.Entity is ICreationAudited)
            {
                entry.Cast<ICreationAudited>().Entity.CreatorUserId = AbpSession.UserId;
            }
        }

        private void PreventSettingCreationAuditProperties(DbEntityEntry entry)
        {
            //TODO@Halil: Implement this when tested well (Issue #49)
            //if (entry.Entity is IHasCreationTime && entry.Cast<IHasCreationTime>().Property(e => e.CreationTime).IsModified)
            //{
            //    throw new DbEntityValidationException(string.Format("Can not change CreationTime on a modified entity {0}", entry.Entity.GetType().FullName));
            //}

            //if (entry.Entity is ICreationAudited && entry.Cast<ICreationAudited>().Property(e => e.CreatorUserId).IsModified)
            //{
            //    throw new DbEntityValidationException(string.Format("Can not change CreatorUserId on a modified entity {0}", entry.Entity.GetType().FullName));
            //}
        }

        private void SetModificationAuditProperties(DbEntityEntry entry)
        {
            if (entry.Entity is IModificationAudited)
            {
                var auditedEntry = entry.Cast<IModificationAudited>();
                auditedEntry.Entity.LastModificationTime = DateTime.Now; //TODO: UtcNow?
                auditedEntry.Entity.LastModifierUserId = AbpSession.UserId;
            }
        }

        private void HandleSoftDelete(DbEntityEntry entry)
        {
            if (entry.Entity is ISoftDelete)
            {
                var softDeleteEntry = entry.Cast<ISoftDelete>();
                softDeleteEntry.State = EntityState.Unchanged;
                softDeleteEntry.Entity.IsDeleted = true;
                if (entry.Entity is IDeletionAudited)
                {
                    var deletionAuditedEntry = entry.Cast<IDeletionAudited>();
                    deletionAuditedEntry.Entity.DeletionTime = DateTime.Now; //TODO: UtcNow?
                    deletionAuditedEntry.Entity.DeleterUserId = AbpSession.UserId;
                }
            }
        }
    }
}
