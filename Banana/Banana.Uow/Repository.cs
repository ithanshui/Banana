﻿/***********************************
 * Coder：EminemJK
 * Date：2018-11-16
 **********************************/

using System;
using System.Collections.Generic;
using Dapper;
using System.Data;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Reflection;
using Banana.Uow.Models;
using Banana.Uow.Interface;

namespace Banana.Uow
{
    /// <summary>
    /// 仓储基类
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        private IDbConnection _dbConnection;
        public IDbConnection DBConnection
        {
            get
            {
                if (_dbConnection == null)
                {
                    _dbConnection = ConnectionBuilder.OpenConnection();
                }
                if (_dbConnection.State == ConnectionState.Closed)
                {
                    _dbConnection.Open();
                }
                return _dbConnection;
            }
            private set { this._dbConnection = value; }
        }

        /// <summary>
        /// 仓储
        /// </summary>
        public Repository()
        {
            _dbConnection = ConnectionBuilder.OpenConnection(); 
        }


        /// <summary>
        /// 仓储
        /// </summary>
        public Repository(IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            this._dbConnection = dbConnection;
            this._dbTransaction = dbTransaction;
        }

        private string _tableName;
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_tableName))
                {
                    _tableName = ((TableAttribute)typeof(T).GetCustomAttribute(typeof(TableAttribute))).Name;
                }
                return _tableName;
            }
        }

        private IDbTransaction _dbTransaction;
        /// <summary>
        /// 开启事务
        /// </summary>
        public IDbTransaction OpenTransaction()
        {
            _dbTransaction = DBConnection.BeginTransaction();
            TrancationState = ETrancationState.Opened;
            return _dbTransaction;
        }

        /// <summary>
        /// 事务状态
        /// </summary>
        public ETrancationState TrancationState { get; private set; } = ETrancationState.Closed;

        /// <summary>
        /// 对象类型
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="entity">实体</param>
        public bool Delete(T entity)
        {
            return DBConnection.Delete(entity, _dbTransaction);
        }

        /// <summary>
        /// 插入实体
        /// </summary>
        /// <param name="entity">实体</param>
        public long Insert(T entity)
        {
            return DBConnection.Insert(entity, _dbTransaction);
        }

        /// <summary>
        /// 查询单个实体
        /// </summary>
        /// <param name="id">Id</param>
        public T Query(int id)
        {
            return DBConnection.Get<T>(id);
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="whereString">where 语句如： name like @name (使用参数化)</param>
        /// <param name="param">参数，语句如：new { name = "%李%" }</param>
        public List<T> QueryList(string whereString = null, object param = null)
        {
            if (string.IsNullOrEmpty(whereString))
            {
                return DBConnection.GetAll<T>().ToList();
            }
            else
            {
                IAdapter adapter = ConnectionBuilder.GetAdapter();
                var sqlbuilder = adapter.GetPageList(this, whereString: whereString, param: param); 
                return DBConnection.Query<T>(sqlbuilder.SQL, sqlbuilder.Arguments).ToList();
            }
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="whereString">where语句，无效携带where关键字</param>
        /// <param name="param">参数</param>
        /// <param name="order">排序</param>
        /// <param name="asc">是否</param>
        /// <returns></returns>
        public List<T> QueryList(int pageNum, int pageSize, string whereString = null, object param = null, string order = null, bool asc = false)
        {
            IAdapter adapter = ConnectionBuilder.GetAdapter();
            var sqlbuilder = adapter.GetPageList(this, pageNum, pageSize, whereString, param, order, asc); 
            return DBConnection.Query<T>(sqlbuilder.SQL, sqlbuilder.Arguments).ToList();
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        public bool Update(T entity)
        {
            return DBConnection.Update<T>(entity, _dbTransaction);
        }

        /// <summary>
        /// 执行单条语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parms"></param>
        public int Execute(string sql, dynamic parms)
        {
            return DBConnection.Execute(sql, (object)parms, transaction: _dbTransaction);
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="entities">entityList</param>
        /// <returns></returns>
        public virtual bool InsertBatch(string sql, IEnumerable<T> entities)
        {
            using (IDbTransaction trans = OpenTransaction())
            {
                try
                {
                    int res = Execute(sql, entities); 
                    if (res > 0)
                    {
                        trans.Commit();
                        TrancationState = ETrancationState.Closed;
                        return true;
                    }
                    else
                    {
                        trans.Rollback();
                        TrancationState = ETrancationState.Closed;
                        return false;
                    }
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                    TrancationState = ETrancationState.Closed;
                    return false;
                }
            }
        }
    }
}
