// 백업 및 복원 유틸리티 클래스
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FinanceTool.Utilities
{
    public class MongoBackupUtility
    {
        // MongoDB 백업 도구 경로 (설치 경로에 맞게 조정 필요)
        private static string mongodumpPath = @"C:\Program Files\MongoDB\Tools\mongodump.exe";
        private static string mongorestorePath = @"C:\Program Files\MongoDB\Tools\mongorestore.exe";

        // 데이터베이스 연결 정보 (MongoDBManager와 공유하는 것이 좋음)
        private static string connectionString = "mongodb://localhost:27017";
        private static string databaseName = "finance_tool";

    }
}