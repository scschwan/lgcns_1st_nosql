using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace FinanceTool.Repositories
{
    public class UploadedFileRepository : BaseRepository<UploadedFileDocument>
    {
        public UploadedFileRepository() : base("uploaded_files")
        {
        }


        // Collection에 직접 접근할 수 있도록 속성 추가
        public IMongoCollection<UploadedFileDocument> Collection => _collection;

        /// <summary>
        /// 파일명으로 파일 조회
        /// </summary>
        public async Task<UploadedFileDocument> GetByFilenameAsync(string filename)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.OriginalFilename, filename);
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일명으로 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 세션별 파일 목록 조회
        /// </summary>
        public async Task<List<UploadedFileDocument>> GetFilesBySessionAsync(ObjectId sessionId)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.SessionId, sessionId);
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션별 파일 조회 오류: {ex.Message}");
                return new List<UploadedFileDocument>();
            }
        }


        /// <summary>
        /// 계정명 컬럼 정보만 업데이트
        /// </summary>
        public async Task<bool> UpdateAccountColumnInfoAsync(ObjectId fileId, string accountColumnName, List<string> accountContents)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", fileId);
                var update = Builders<UploadedFileDocument>.Update
                    .Set("account_column_name", accountColumnName)
                    .Set("account_contents", accountContents);

                var result = await Collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"계정명 컬럼 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 금액 컬럼 정보만 업데이트
        /// </summary>
        public async Task<bool> UpdateAmountColumnInfoAsync(ObjectId fileId, string amountColumnName, decimal totalAmount)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", fileId);
                var update = Builders<UploadedFileDocument>.Update
                    .Set("amount_column_name", amountColumnName)
                    .Set("total_amount", totalAmount);

                var result = await Collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"금액 컬럼 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ObjectId로 직접 조회
        /// </summary>
        public async Task<UploadedFileDocument> GetByIdAsync(ObjectId id)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", id);
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
        /// 컬럼 정보 업데이트 (계정명, 금액 컬럼)
        /// </summary>
        public async Task<bool> UpdateColumnInfoAsync(ObjectId fileId, string accountColumnName, string amountColumnName, decimal totalAmount)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.Id, fileId);
                var update = Builders<UploadedFileDocument>.Update
                    .Set(d => d.AccountColumnName, accountColumnName)
                    .Set(d => d.AmountColumnName, amountColumnName)
                    .Set(d => d.TotalAmount, totalAmount);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 파일의 컬럼 정보 업데이트 (간소화 버전)
        /// </summary>
        public async Task<bool> UpdateColumnInfoWithAccountContentsAsync(ObjectId fileId,
            string accountColumnName, string amountColumnName, decimal totalAmount, List<string> accountContents)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.Id, fileId);
                var update = Builders<UploadedFileDocument>.Update
                    .Set(d => d.AccountColumnName, accountColumnName)
                    .Set(d => d.AmountColumnName, amountColumnName)
                    .Set(d => d.TotalAmount, totalAmount)
                    .Set(d => d.AccountContents, accountContents);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 파일의 세션 ID 업데이트
        /// </summary>
        public async Task<bool> UpdateSessionIdAsync(ObjectId fileId, ObjectId sessionId)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.Id, fileId);

                UpdateDefinition<UploadedFileDocument> update;

                // sessionId가 null이거나 Empty인 경우 필드를 제거 (null로 설정)
                if (sessionId == null || sessionId == ObjectId.Empty)
                {
                    update = Builders<UploadedFileDocument>.Update.Unset(d => d.SessionId);
                }
                else
                {
                    update = Builders<UploadedFileDocument>.Update.Set(d => d.SessionId, sessionId);
                }

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 ID 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 처리 상태 업데이트
        /// </summary>
        public async Task<bool> UpdateProcessingStatusAsync(ObjectId fileId, string status)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.Id, fileId);
                var update = Builders<UploadedFileDocument>.Update.Set(d => d.ProcessingStatus, status);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"처리 상태 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 동일한 계정명/금액 컬럼을 가진 파일들 조회
        /// </summary>
        public async Task<List<UploadedFileDocument>> GetFilesByColumnNamesAsync(string accountColumnName, string amountColumnName)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.And(
                    Builders<UploadedFileDocument>.Filter.Eq(d => d.AccountColumnName, accountColumnName),
                    Builders<UploadedFileDocument>.Filter.Eq(d => d.AmountColumnName, amountColumnName)
                );
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼명별 파일 조회 오류: {ex.Message}");
                return new List<UploadedFileDocument>();
            }
        }


        /// <summary>
        /// 세션이 할당되지 않은 파일들 조회
        /// </summary>
        public async Task<List<UploadedFileDocument>> GetUnassignedFilesAsync()
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Or(
                    Builders<UploadedFileDocument>.Filter.Eq(d => d.SessionId, null),
                    Builders<UploadedFileDocument>.Filter.Exists(d => d.SessionId, false)
                );
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"미할당 파일 조회 오류: {ex.Message}");
                return new List<UploadedFileDocument>();
            }
        }
    }
}