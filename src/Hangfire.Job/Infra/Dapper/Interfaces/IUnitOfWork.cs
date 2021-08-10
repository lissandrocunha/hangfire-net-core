﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Hangfire.Job.Infra.Dapper.Interfaces
{
    public interface IUnitOfWork: IDisposable
    {

        Guid Id { get; }
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        void Begin();
        void Commit();
        void Rollback();

        T Repository<T>() where T : class;
    }
}
