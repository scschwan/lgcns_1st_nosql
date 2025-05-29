using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// process_view_data 컬렉션을 위한 저장소 클래스 (개선버전)
    /// </summary>
    public class ProcessViewRepository : BaseRepository<ProcessViewDocument>
    {
        public ProcessViewRepository() : base("process_view_data")
        {
        }

        /// <summary>
        /// ProcessData ID로 관련 ProcessView 문서 찾기
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByProcessDataIdAsync(string processDataId)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq(d => d.ProcessDataId, processDataId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// RawData ID로 관련 ProcessView 문서 찾기 (추가됨)
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByRawDataIdAsync(string rawDataId)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq(d => d.RawDataId, rawDataId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 여러 ProcessView 문서를 배치로 삽입 (InsertManyOptions 지원)
        /// </summary>
        public async Task InsertManyAsync(List<ProcessViewDocument> documents, InsertManyOptions options)
        {
            if (documents == null || documents.Count == 0)
                return;

            try
            {
                // MongoDB 연결 상태 확인
                await InitializeAsync();

                // 대용량 데이터 처리를 위한 WriteConcern 최적화
                var optimizedOptions = new InsertManyOptions
                {
                    IsOrdered = options?.IsOrdered ?? false, // 순서 상관없이 삽입하여 성능 향상
                    BypassDocumentValidation = options?.BypassDocumentValidation ?? false
                };

                // 배치 삽입 실행
                await _collection.InsertManyAsync(documents, optimizedOptions);

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ProcessView 문서 {documents.Count}개 삽입 완료");
            }
            catch (MongoBulkWriteException ex)
            {
                // 부분적 실패 처리
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 삽입 중 일부 문서 실패: {ex.WriteErrors?.Count ?? 0}개 오류");

                // 성공한 문서 수 로깅
                int successCount = documents.Count - (ex.WriteErrors?.Count ?? 0);
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 성공적으로 삽입된 문서: {successCount}개");

                // 실패한 문서들에 대한 세부 정보 로깅
                if (ex.WriteErrors != null)
                {
                    foreach (var error in ex.WriteErrors)
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 삽입 실패 - 인덱스: {error.Index}, 오류: {error.Message}");
                    }
                }

                throw; // 상위 호출자에게 예외 전파
            }
           
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 예상치 못한 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 여러 ProcessView 문서를 배치로 삽입 (기존 메서드 - 하위 호환성 유지)
        /// </summary>
        public async Task InsertManyAsync(List<ProcessViewDocument> documents)
        {
            // 기본 옵션으로 오버로드된 메서드 호출
            var defaultOptions = new InsertManyOptions
            {
                IsOrdered = false, // 성능 최적화를 위해 기본값을 false로 설정
                BypassDocumentValidation = false
            };

            await InsertManyAsync(documents, defaultOptions);
        }

        /// <summary>
        /// 재시도 로직이 포함된 안전한 배치 삽입
        /// </summary>
        public async Task InsertManyWithRetryAsync(List<ProcessViewDocument> documents, InsertManyOptions options = null, int maxRetries = 3)
        {
            if (documents == null || documents.Count == 0)
                return;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await InsertManyAsync(documents, options);
                    return; // 성공하면 즉시 반환
                }
                
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 복구 불가능한 오류 발생: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 개별 문서 삽입 (배치 삽입 실패 시 fallback 용도)
        /// </summary>
        public async Task<int> InsertManyIndividuallyAsync(List<ProcessViewDocument> documents)
        {
            if (documents == null || documents.Count == 0)
                return 0;

            int successCount = 0;
            var tasks = new List<Task<bool>>();

            // 동시성 제한을 위한 SemaphoreSlim 사용
            using (var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2))
            {
                foreach (var document in documents)
                {
                    tasks.Add(InsertOneWithSemaphoreAsync(document, semaphore));
                }

                var results = await Task.WhenAll(tasks);
                successCount = results.Count(r => r);
            }

            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 개별 삽입 완료: {successCount}/{documents.Count}개 성공");
            return successCount;
        }

        /// <summary>
        /// 세마포어를 사용한 개별 문서 삽입 (동시성 제어)
        /// </summary>
        private async Task<bool> InsertOneWithSemaphoreAsync(ProcessViewDocument document, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                return await InsertOneAsync(document);
            }
            finally
            {
                semaphore.Release();
            }
        }


        public async Task<long> CountDocumentsAsync(FilterDefinition<ProcessViewDocument> filter = null)
        {
            filter = filter ?? Builders<ProcessViewDocument>.Filter.Empty;
            return await _collection.CountDocumentsAsync(filter);
        }

        

        /// <summary>
        /// 단일 ProcessView 문서를 삽입합니다.
        /// </summary>
        public async Task<bool> InsertOneAsync(ProcessViewDocument document)
        {
            try
            {
                await _collection.InsertOneAsync(document);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertOneAsync 오류: {ex.Message}");
                return false;
            }
        }
    }
}