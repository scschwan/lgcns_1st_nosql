using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace FinanceTool.Repositories
{
    public class FileSessionRepository : BaseRepository<FileSessionDocument>
    {
        public FileSessionRepository() : base("file_sessions")
        {
        }

        /// <summary>
        /// 세션명으로 세션 조회
        /// </summary>
        public async Task<FileSessionDocument> GetBySessionNameAsync(string sessionName)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(d => d.SessionName, sessionName);
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션명으로 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 계정명과 금액 컬럼명으로 기존 세션 찾기
        /// </summary>
        public async Task<FileSessionDocument> FindMatchingSessionAsync(string accountColumnName, string amountColumnName)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.And(
                    Builders<FileSessionDocument>.Filter.Eq(d => d.AccountColumnName, accountColumnName),
                    Builders<FileSessionDocument>.Filter.Eq(d => d.AmountColumnName, amountColumnName),
                    Builders<FileSessionDocument>.Filter.Ne(d => d.Status, "completed")
                );
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"매칭 세션 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 세션에 파일 추가
        /// </summary>
        public async Task<bool> AddFileToSessionAsync(ObjectId sessionId, ObjectId fileId)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(d => d.Id, sessionId);
                var update = Builders<FileSessionDocument>.Update.AddToSet(d => d.FileIds, fileId);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션에 파일 추가 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션의 총합 금액과 행수 업데이트
        /// </summary>
        public async Task<bool> UpdateSessionTotalsAsync(ObjectId sessionId, decimal totalAmount, decimal totalRows)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(d => d.Id, sessionId);
                var update = Builders<FileSessionDocument>.Update
                    .Set(d => d.TotalAmount, totalAmount)
                    .Set(d => d.TotalRows, totalRows);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 총합 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션 상태 업데이트
        /// </summary>
        public async Task<bool> UpdateSessionStatusAsync(ObjectId sessionId, string status, string resultFilePath = null)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(d => d.Id, sessionId);
                var updateBuilder = Builders<FileSessionDocument>.Update.Set(d => d.Status, status);

                if (status == "completed")
                {
                    updateBuilder = updateBuilder.Set(d => d.CompletedDate, DateTime.UtcNow);
                    if (!string.IsNullOrEmpty(resultFilePath))
                    {
                        updateBuilder = updateBuilder.Set(d => d.ResultFilePath, resultFilePath);
                    }
                }

                var result = await _collection.UpdateOneAsync(filter, updateBuilder);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 상태 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 모든 활성 세션 조회 (완료되지 않은 세션들)
        /// </summary>
        public async Task<List<FileSessionDocument>> GetActiveSessionsAsync()
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Ne(d => d.Status, "completed");
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"활성 세션 조회 오류: {ex.Message}");
                return new List<FileSessionDocument>();
            }
        }
    }
}