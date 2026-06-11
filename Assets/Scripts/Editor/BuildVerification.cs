using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// 构建验证工具 - 验证DebugLogger条件编译在生产构建中的效果
/// </summary>
public class BuildVerification
{
    /// <summary>
    /// 构建并验证生产版本（Release构建）
    /// </summary>
    [MenuItem("Build/Verify Production Build (Release)")]
    public static void VerifyProductionBuild()
    {
        ConsoleLog("开始构建生产版本验证...", LogType.Log);
        
        // 配置Release构建设置
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        ConsoleLog($"构建设置: Development={EditorUserBuildSettings.development}, ScriptDebugging={EditorUserBuildSettings.allowDebugging}", LogType.Log);
        
        // 执行构建
        string buildPath = "Build/SurvivorLike0_ProductionVerify.exe";
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/GameLevel.scene" },
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC
        };
        
        ConsoleLog($"构建目标: {buildPath}", LogType.Log);
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        
        // 分析构建结果
        ConsoleLog($"构建结果: {report.summary.result}", LogType.Log);
        ConsoleLog($"构建时间: {report.summary.totalTime}", LogType.Log);
        ConsoleLog($"构建大小: {report.summary.totalSize} bytes ({(report.summary.totalSize / 1024.0 / 1024.0):F2} MB)", LogType.Log);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            ConsoleLog("✓ 生产版本构建成功！", LogType.Log);
            
            // 检查构建输出
            if (File.Exists(buildPath))
            {
                FileInfo fileInfo = new FileInfo(buildPath);
                ConsoleLog($"✓ 可执行文件已生成: {fileInfo.Length} bytes", LogType.Log);
                
                // 创建验证报告
                CreateVerificationReport(report, buildPath, "Production");
                
                ConsoleLog("验证步骤:", LogType.Log);
                ConsoleLog("1. 运行生成的exe文件", LogType.Log);
                ConsoleLog("2. 测试游戏功能是否正常", LogType.Log);
                ConsoleLog("3. 观察控制台是否无日志输出", LogType.Log);
                ConsoleLog("4. 确认DebugLogger.Log调用在生产环境中被移除", LogType.Log);
                
                // 尝试打开文件所在目录
                string directory = Path.GetDirectoryName(buildPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    EditorUtility.RevealInFinder(buildPath);
                    ConsoleLog($"已打开构建目录: {directory}", LogType.Log);
                }
                
                ConsoleLog("✓ 请按上述步骤完成验证", LogType.Log);
            }
            else
            {
                ConsoleLog("✗ 构建文件未找到", LogType.Error);
            }
        }
        else
        {
            ConsoleLog("✗ 构建失败", LogType.Error);
            
            // 显示详细错误信息
            ConsoleLog("构建错误详情:", LogType.Error);
            foreach (var step in report.steps)
            {
                var errors = step.messages.Where(m => m.type == LogType.Error).ToList();
                if (errors.Any())
                {
                    ConsoleLog($"步骤: {step.name}", LogType.Error);
                    foreach (var error in errors)
                    {
                        ConsoleLog($"  {error.content}", LogType.Error);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 构建并验证开发版本（Development构建，关闭Script Debugging）
    /// </summary>
    [MenuItem("Build/Verify Development Build")]
    public static void VerifyDevelopmentBuild()
    {
        ConsoleLog("开始构建开发版本验证...", LogType.Log);
        
        // 配置Development构建设置
        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.allowDebugging = false;
        ConsoleLog($"构建设置: Development={EditorUserBuildSettings.development}, ScriptDebugging={EditorUserBuildSettings.allowDebugging}", LogType.Log);
        
        // 执行构建
        string buildPath = "Build/SurvivorLike0_DevelopmentVerify.exe";
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/GameLevel.scene" },
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC | BuildOptions.Development
        };
        
        ConsoleLog($"构建目标: {buildPath}", LogType.Log);
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        
        // 分析构建结果
        ConsoleLog($"构建结果: {report.summary.result}", LogType.Log);
        ConsoleLog($"构建时间: {report.summary.totalTime}", LogType.Log);
        ConsoleLog($"构建大小: {report.summary.totalSize} bytes ({(report.summary.totalSize / 1024.0 / 1024.0):F2} MB)", LogType.Log);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            ConsoleLog("✓ 开发版本构建成功！", LogType.Log);
            
            if (File.Exists(buildPath))
            {
                FileInfo fileInfo = new FileInfo(buildPath);
                ConsoleLog($"✓ 可执行文件已生成: {fileInfo.Length} bytes", LogType.Log);
                
                // 创建验证报告
                CreateVerificationReport(report, buildPath, "Development");
                
                ConsoleLog("开发版本验证步骤:", LogType.Log);
                ConsoleLog("1. 运行生成的exe文件", LogType.Log);
                ConsoleLog("2. 测试游戏功能是否正常", LogType.Log);
                ConsoleLog("3. 在开发模式中日志应该正常工作", LogType.Log);
                
                // 尝试打开文件所在目录
                string directory = Path.GetDirectoryName(buildPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    EditorUtility.RevealInFinder(buildPath);
                    ConsoleLog($"已打开构建目录: {directory}", LogType.Log);
                }
                
                ConsoleLog("✓ 请按上述步骤完成验证", LogType.Log);
            }
        }
        else
        {
            ConsoleLog("✗ 构建失败", LogType.Error);
            DisplayBuildErrors(report);
        }
    }
    
    /// <summary>
    /// 创建验证报告文件
    /// </summary>
    private static void CreateVerificationReport(BuildReport report, string buildPath, string buildType)
    {
        string reportPath = Path.Combine(Application.dataPath, "..", ".codely", "BuildVerificationReport.md");
        
        try
        {
            string reportContent = $@"# Task 1 构建验证报告

## 构建信息
- **验证时间**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
- **构建类型**: {buildType}
- **构建目标**: StandaloneWindows64
- **输出路径**: {buildPath}
- **构建大小**: {report.summary.totalSize} bytes ({(report.summary.totalSize / 1024.0 / 1024.0):F2} MB)
- **构建时间**: {report.summary.totalTime}
- **构建状态**: {report.summary.result}

## 构建设置
- **Development Build**: {EditorUserBuildSettings.development}
- **Script Debugging**: {EditorUserBuildSettings.allowDebugging}
- **压缩方法**: LZ4HC

## 验证目标
Task 1目标：移除游戏运行时不必要的Debug.Log调用，减少生产环境的I/O开销

## 验证方法
使用条件编译属性 `[System.Diagnostics.Conditional(""UNITY_EDITOR"")]` 确保DebugLogger.Log调用在生产构建中被编译器移除。

## 验证步骤
1. ✅ 构建成功 - 可执行文件已生成
2. ⏳ 运行游戏测试功能
3. ⏳ 检查生产环境日志输出
4. ⏳ 确认DebugLogger.Log调用移除效果

## 测试清单
- [ ] 游戏启动正常
- [ ] 玩家移动正常
- [ ] 武器系统工作正常
- [ ] 敌人生成正常
- [ ] 升级系统工作正常
- [ ] 掉落收集正常
- [ ] 游戏结束流程正常
- [ ] 生产环境无控制台日志输出

## 技术验证
**Code analysis**: DebugLogger.cs使用ConditionalAttribute
**Expected behavior**: 
- {buildType}模式中 {("DEBUG".Equals(buildType, StringComparison.OrdinalIgnoreCase) ? "日志应正常工作" : "日志调用应完全移除")}
- 零运行时I/O开销

## 验证结果
*(请在此记录实际测试结果)*

测试人员: ___________
测试日期: ___________
测试结果: [ ] 通过  [ ] 失败
备注: ____________________

---

**Task 1 实现人员**: 子代理  
**验证时间**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";

            string directory = Path.GetDirectoryName(reportPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(reportPath, reportContent);
            ConsoleLog($"✓ 验证报告已生成: {reportPath}", LogType.Log);
        }
        catch (Exception e)
        {
            ConsoleLog($"✗ 创建验证报告失败: {e.Message}", LogType.Error);
        }
    }
    
    /// <summary>
    /// 显示构建错误详情
    /// </summary>
    private static void DisplayBuildErrors(BuildReport report)
    {
        ConsoleLog("构建错误详情:", LogType.Error);
        foreach (var step in report.steps)
        {
            var errors = step.messages.Where(m => m.type == LogType.Error).ToList();
            if (errors.Any())
            {
                ConsoleLog($"步骤: {step.name}", LogType.Error);
                foreach (var error in errors)
                {
                    ConsoleLog($"  {error.content}", LogType.Error);
                }
            }
        }
    }
    
    /// <summary>
    /// 控制台输出辅助方法
    /// </summary>
    private static void ConsoleLog(string message, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                Debug.LogError($"[BuildVerification] {message}");
                break;
            case LogType.Warning:
                Debug.LogWarning($"[BuildVerification] {message}");
                break;
            case LogType.Log:
            default:
                Debug.Log($"[BuildVerification] {message}");
                break;
        }
    }
}