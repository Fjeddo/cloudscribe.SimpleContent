﻿using cloudscribe.SimpleContent.Storage.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SimpleContentEFStartup
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetService<ISimpleContentDbContext>();
            await db.Database.MigrateAsync();
        }
    }
}
