using FinanceTool.Models.MongoModels;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// process_data 컬렉션에 대한 특화된 저장소
    /// </summary>
    public class ProcessDataRepository : BaseRepository<ProcessDataDocument>
    {
        public ProcessDataRepository() : base("process_data")
        {
        }

       
    }
}