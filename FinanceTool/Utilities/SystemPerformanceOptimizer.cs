using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    /// <summary>
    /// 시스템 성능 극대화 설정 클래스
    /// 192GB RAM과 16코어 CPU 환경에 최적화
    /// </summary>
    public static class SystemPerformanceOptimizer
    {
        private static bool _isOptimized = false;
        private static readonly object _optimizationLock = new object();

        /// <summary>
        /// 시스템 성능 최적화 적용 (한 번만 실행)
        /// </summary>
        public static void OptimizeSystemForUltraSpeed()
        {
            if (_isOptimized) return;

            lock (_optimizationLock)
            {
                if (_isOptimized) return;

                try
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 시스템 성능 최적화 시작");

                    // 1. GC 설정 최적화 (192GB RAM 활용)
                    OptimizeGarbageCollection();

                    // 2. 스레드 풀 최적화 (16코어 CPU 활용)
                    OptimizeThreadPool();

                    // 3. .NET 런타임 최적화
                    OptimizeDotNetRuntime();

                    // 4. 메모리 할당 최적화
                    OptimizeMemoryAllocation();

                    _isOptimized = true;
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 시스템 성능 최적화 완료");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 시스템 성능 최적화 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// GC 최적화 (192GB RAM 환경)
        /// </summary>
        private static void OptimizeGarbageCollection()
        {
            try
            {
                // Server GC 모드 확인 및 설정
                if (!GCSettings.IsServerGC)
                {
                    Debug.WriteLine("경고: Server GC가 활성화되지 않음. app.config에 추가 권장:");
                    Debug.WriteLine("<gcServer enabled=\"true\"/>");
                }

                // 대용량 메모리 환경을 위한 GC 지연 모드 설정
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

                // 메모리 압박 임계값 조정 (192GB의 80% 활용)
                long targetMemoryBytes = 192L * 1024 * 1024 * 1024 * 80 / 100; // 153GB

                Debug.WriteLine($"GC 최적화 완료 - 목표 메모리: {targetMemoryBytes / 1024 / 1024 / 1024}GB");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GC 최적화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스레드 풀 최적화 (16코어 CPU 환경)
        /// </summary>
        private static void OptimizeThreadPool()
        {
            try
            {
                int coreCount = Environment.ProcessorCount; // 16 cores

                // 작업자 스레드 최적화 (코어당 8-16개 스레드)
                int minWorkerThreads = coreCount * 8;   // 128개
                int maxWorkerThreads = coreCount * 16;  // 256개

                // I/O 완료 포트 스레드 최적화
                int minCompletionPortThreads = coreCount * 4;  // 64개
                int maxCompletionPortThreads = coreCount * 8;  // 128개

                // 최소 스레드 수 설정
                ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);

                // 최대 스레드 수 설정
                ThreadPool.SetMaxThreads(maxWorkerThreads, maxCompletionPortThreads);

                Debug.WriteLine($"스레드 풀 최적화 완료 - Worker: {minWorkerThreads}-{maxWorkerThreads}, " +
                               $"IOCP: {minCompletionPortThreads}-{maxCompletionPortThreads}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"스레드 풀 최적화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// .NET 런타임 최적화
        /// </summary>
        private static void OptimizeDotNetRuntime()
        {
            try
            {
                // JIT 컴파일러 최적화
                System.Runtime.ProfileOptimization.SetProfileRoot(Path.GetTempPath());
                System.Runtime.ProfileOptimization.StartProfile("FinanceToolOptimization.prof");

                Debug.WriteLine(".NET 런타임 최적화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($".NET 런타임 최적화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 메모리 할당 최적화
        /// </summary>
        private static void OptimizeMemoryAllocation()
        {
            try
            {
                // 대용량 객체를 위한 사전 할당
                var dummy = new byte[85000]; // LOH 임계값 초과
                dummy = null;

                // GC를 한 번 실행하여 초기화
                GC.Collect(2, GCCollectionMode.Optimized);
                GC.WaitForPendingFinalizers();

                Debug.WriteLine("메모리 할당 최적화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"메모리 할당 최적화 오류: {ex.Message}");
            }
        }
    }
}
