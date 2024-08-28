using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System;

namespace DataAccess
{
    public static class DbContextFactory
    {
        public static T GetContext<T>(string connectionString, IBaseContextAccesor ctx) where T : BaseContext
        {
            DbContextOptionsBuilder<T> opts = new();
            CollectionExtension.configureConnectionString(connectionString, opts);
            return (T)Activator.CreateInstance(typeof(T), new object[] { opts.Options, ctx });
        }
    }
}