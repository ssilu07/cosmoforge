// ============================================================================
// CosmoForge Test Framework: Headless BatchMode Test Runner
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CosmoForge.Tests
{
    /// <summary>
    /// Headless CLI and BatchMode test runner for executing all E2E and Unit test suites
    /// in automated environments (-batchmode -nographics -executeMethod CosmoForge.Tests.HeadlessTestRunner.RunAllTestsHeadless).
    /// </summary>
    public static class HeadlessTestRunner
    {
        public class TestResultRecord
        {
            public string FixtureName;
            public string TestName;
            public bool Passed;
            public double DurationSeconds;
            public string ErrorMessage;
            public string StackTrace;
        }

        public class TestSuiteSummary
        {
            public int TotalTests;
            public int Passed;
            public int Failed;
            public double TotalDurationSeconds;
            public string Timestamp;
            public List<TestResultRecord> Results = new List<TestResultRecord>();
        }

        /// <summary>
        /// Entry point for batch mode execution:
        /// -executeMethod CosmoForge.Tests.HeadlessTestRunner.RunAllTestsHeadless
        /// </summary>
        public static void RunAllTestsHeadless()
        {
            UnityEngine.Debug.Log("=================================================================");
            UnityEngine.Debug.Log("[CosmoForge Test Runner] Starting Headless Test Suite Execution...");
            UnityEngine.Debug.Log("=================================================================");

            var summary = new TestSuiteSummary
            {
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var overallStopwatch = Stopwatch.StartNew();

            // Locate test assembly
            Assembly testAssembly = typeof(HeadlessTestRunner).Assembly;
            Type[] types = testAssembly.GetTypes();

            List<Type> fixtureTypes = new List<Type>();
            foreach (var type in types)
            {
                if (type.GetCustomAttribute<TestFixtureAttribute>() != null ||
                    type.Name.EndsWith("Tests") || type.Name.EndsWith("Test"))
                {
                    fixtureTypes.Add(type);
                }
            }

            UnityEngine.Debug.Log($"[CosmoForge Test Runner] Discovered {fixtureTypes.Count} test fixtures in {testAssembly.GetName().Name}.");

            foreach (var fixtureType in fixtureTypes)
            {
                ExecuteFixture(fixtureType, summary);
            }

            overallStopwatch.Stop();
            summary.TotalDurationSeconds = overallStopwatch.Elapsed.TotalSeconds;

            // Log Summary
            UnityEngine.Debug.Log("=================================================================");
            UnityEngine.Debug.Log($"[CosmoForge Test Runner] Execution Complete in {summary.TotalDurationSeconds:F2} seconds.");
            UnityEngine.Debug.Log($"[CosmoForge Test Runner] Total: {summary.TotalTests} | Passed: {summary.Passed} | Failed: {summary.Failed}");
            UnityEngine.Debug.Log("=================================================================");

            // Write Output Reports
            WriteJsonReport(summary);
            WriteXmlReport(summary);

            int exitCode = summary.Failed == 0 ? 0 : 1;
#if UNITY_EDITOR
            EditorApplication.Exit(exitCode);
#else
            Environment.Exit(exitCode);
#endif
        }

        private static void ExecuteFixture(Type fixtureType, TestSuiteSummary summary)
        {
            MethodInfo[] methods = fixtureType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            List<MethodInfo> testMethods = new List<MethodInfo>();
            MethodInfo setUpMethod = null;
            MethodInfo tearDownMethod = null;

            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<SetUpAttribute>() != null)
                {
                    setUpMethod = method;
                }
                else if (method.GetCustomAttribute<TearDownAttribute>() != null)
                {
                    tearDownMethod = method;
                }
                else if (method.GetCustomAttribute<TestAttribute>() != null ||
                         method.Name.StartsWith("Test_") ||
                         method.Name.StartsWith("F0") ||
                         method.Name.StartsWith("F1") ||
                         method.Name.StartsWith("F2") ||
                         method.Name.StartsWith("T3_") ||
                         method.Name.StartsWith("S"))
                {
                    testMethods.Add(method);
                }
            }

            if (testMethods.Count == 0) return;

            UnityEngine.Debug.Log($"[Fixture] {fixtureType.Name} ({testMethods.Count} tests)");

            foreach (var testMethod in testMethods)
            {
                summary.TotalTests++;
                object fixtureInstance = null;
                var record = new TestResultRecord
                {
                    FixtureName = fixtureType.Name,
                    TestName = testMethod.Name
                };

                var testStopwatch = Stopwatch.StartNew();

                try
                {
                    fixtureInstance = Activator.CreateInstance(fixtureType);
                    setUpMethod?.Invoke(fixtureInstance, null);

                    testMethod.Invoke(fixtureInstance, null);

                    testStopwatch.Stop();
                    record.Passed = true;
                    record.DurationSeconds = testStopwatch.Elapsed.TotalSeconds;
                    summary.Passed++;
                    UnityEngine.Debug.Log($"  [PASS] {testMethod.Name} ({record.DurationSeconds * 1000.0:F1} ms)");
                }
                catch (TargetInvocationException ex)
                {
                    testStopwatch.Stop();
                    record.Passed = false;
                    record.DurationSeconds = testStopwatch.Elapsed.TotalSeconds;
                    record.ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    record.StackTrace = ex.InnerException != null ? ex.InnerException.StackTrace : ex.StackTrace;
                    summary.Failed++;
                    UnityEngine.Debug.LogError($"  [FAIL] {testMethod.Name}: {record.ErrorMessage}\n{record.StackTrace}");
                }
                catch (Exception ex)
                {
                    testStopwatch.Stop();
                    record.Passed = false;
                    record.DurationSeconds = testStopwatch.Elapsed.TotalSeconds;
                    record.ErrorMessage = ex.Message;
                    record.StackTrace = ex.StackTrace;
                    summary.Failed++;
                    UnityEngine.Debug.LogError($"  [FAIL] {testMethod.Name}: {record.ErrorMessage}\n{record.StackTrace}");
                }
                finally
                {
                    try
                    {
                        tearDownMethod?.Invoke(fixtureInstance, null);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[TearDown Warning] {fixtureType.Name}.{testMethod.Name}: {ex.Message}");
                    }
                    summary.Results.Add(record);
                }
            }
        }

        private static void WriteJsonReport(TestSuiteSummary summary)
        {
            try
            {
                string json = JsonUtility.ToJson(summary, true);
                File.WriteAllText("TestResults.json", json, Encoding.UTF8);
                UnityEngine.Debug.Log("[CosmoForge Test Runner] Wrote TestResults.json");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Test Runner] Failed to write TestResults.json: {ex.Message}");
            }
        }

        private static void WriteXmlReport(TestSuiteSummary summary)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.AppendLine($"<test-run id=\"2\" testcasecount=\"{summary.TotalTests}\" result=\"{(summary.Failed == 0 ? "Passed" : "Failed")}\" total=\"{summary.TotalTests}\" passed=\"{summary.Passed}\" failed=\"{summary.Failed}\" duration=\"{summary.TotalDurationSeconds:F4}\" time=\"{summary.Timestamp}\">");
                sb.AppendLine($"  <test-suite type=\"Assembly\" name=\"CosmoForge.Tests\" total=\"{summary.TotalTests}\" passed=\"{summary.Passed}\" failed=\"{summary.Failed}\">");

                foreach (var res in summary.Results)
                {
                    sb.AppendLine($"    <test-case name=\"{res.FixtureName}.{res.TestName}\" result=\"{(res.Passed ? "Passed" : "Failed")}\" time=\"{res.DurationSeconds:F4}\">");
                    if (!res.Passed)
                    {
                        sb.AppendLine("      <failure>");
                        sb.AppendLine($"        <message><![CDATA[{res.ErrorMessage}]]></message>");
                        sb.AppendLine($"        <stack-trace><![CDATA[{res.StackTrace}]]></stack-trace>");
                        sb.AppendLine("      </failure>");
                    }
                    sb.AppendLine("    </test-case>");
                }

                sb.AppendLine("  </test-suite>");
                sb.AppendLine("</test-run>");

                File.WriteAllText("TestResults.xml", sb.ToString(), Encoding.UTF8);
                UnityEngine.Debug.Log("[CosmoForge Test Runner] Wrote TestResults.xml");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Test Runner] Failed to write TestResults.xml: {ex.Message}");
            }
        }
    }
}
