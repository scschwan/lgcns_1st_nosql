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
        /// 세션 완료 정보 업데이트
        /// </summary>
        public async Task<bool> UpdateSessionCompletionAsync(ObjectId sessionId, string status, DateTime completedDate, string resultFilePath)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(s => s.Id, sessionId);
                var update = Builders<FileSessionDocument>.Update
                    .Set(s => s.Status, status)
                    .Set(s => s.CompletedDate, completedDate)
                    .Set(s => s.ResultFilePath, resultFilePath);

                var result = await _collection.UpdateOneAsync(filter, update);

                Debug.WriteLine($"세션 업데이트 결과 - ModifiedCount: {result.ModifiedCount}, MatchedCount: {result.MatchedCount}");

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 완료 정보 업데이트 중 오류: {ex.Message}");
                return false;
            }
        }

        // <summary>
        /// 세션명 업데이트
        /// </summary>
        public async Task<bool> UpdateSessionNameAsync(ObjectId sessionId, string newSessionName)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq("_id", sessionId);
                var update = Builders<FileSessionDocument>.Update.Set("session_name", newSessionName);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션명 업데이트 오류: {ex.Message}");
                return false;
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
        /// ObjectId로 직접 조회
        /// </summary>
        public async Task<FileSessionDocument> GetByIdAsync(ObjectId id)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq("_id", id);
                var cursor = await _collection.FindAsync(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 병합된 세션 정보 업데이트
        /// </summary>
        public async Task<bool> UpdateMergedSessionAsync(
            ObjectId sessionId,
            string mergedSessionName,
            string mergedAccountName,
            string accountColumnName,
            List<ObjectId> allFileIds,
            decimal totalAmount,
            decimal totalRows)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq("_id", sessionId);
                var update = Builders<FileSessionDocument>.Update
                    .Set("session_name", mergedSessionName)
                    .Set("account_name", mergedAccountName)
                    .Set("account_column_name", accountColumnName)
                    .Set("file_ids", allFileIds)
                    .Set("total_amount", totalAmount)
                    .Set("total_rows", totalRows)
                    .Set("file_count", allFileIds.Count)
                    .Set("updated_date", DateTime.UtcNow);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"병합된 세션 업데이트 오류: {ex.Message}");
                return false;
            }
        }

       
    }
}