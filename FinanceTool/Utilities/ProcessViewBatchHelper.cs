using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using FinanceTool.MongoModels;
using System.Linq;
using System.Diagnostics;

namespace FinanceTool.Utilities
{
    /// <summary>
    /// ProcessView 문서 배치 처리를 위한 도우미 클래스
    /// </summary>
    public static class ProcessViewBatchHelper
    {
        /// <summary>
        /// 데이터를 배치로 나누어 병렬 처리
        /// </summary>
        public static async Task ProcessBatchesAsync<T>(
            List<T> items,
            Func<List<T>, Task> processBatchFunc,
            int batchSize = 1000,
            int maxDegreeOfParallelism = 0,
            IProgress<int> progress = null)
        {
            if (items == null || items.Count == 0) return;

            // 배치 크기 및 병렬 처리 수준 결정
            maxDegreeOfParallelism = maxDegreeOfParallelism <= 0
                ? Math.Max(1, Environment.ProcessorCount - 1)
                : maxDegreeOfParallelism;

            // 배치로 분할
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                batches.Add(items.Skip(i).Take(Math.Min(batchSize, items.Count - i)).ToList());
            }

            Debug.WriteLine($"총 {items.Count}개 항목을 {batches.Count}개 배치로 분할, 병렬 수준: {maxDegreeOfParallelism}");

            // 실행 중인 작업 목록
            var runningTasks = new List<Task>();
            var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
            var completedBatches = 0;

            foreach (var batch in batches)
            {
                await throttler.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await processBatchFunc(batch);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"배치 처리 중 오류 발생: {ex.Message}");
                    }
                    finally
                    {
                        throttler.Release();

                        // 진행 상황 업데이트
                        int batchesCompleted = Interlocked.Increment(ref completedBatches);
                        int percentage = (int)((double)batchesCompleted / batches.Count * 100);
                        progress?.Report(percentage);
                    }
                });

                runningTasks.Add(task);
            }

            // 모든 작업 완료 대기
            await Task.WhenAll(runningTasks);
        }
    }
}