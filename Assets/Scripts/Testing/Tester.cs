using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework.Internal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Testing
{
    public class Tester
    {
        private static readonly ConcurrentQueue<TestReport> s_testResults = new ConcurrentQueue<TestReport>();
        private static TestWriter s_testWriter;

        public static void Init(DirectoryInfo testFolderPath, string testFileName)
        {
            s_testWriter = new TestWriter(testFolderPath, testFileName);

            Application.quitting += () =>
            {
                s_testWriter.Dispose();
            };
        }

        public static void Flush()
        {
            while (s_testResults.TryDequeue(out TestReport report))
            {
                s_testWriter.WriteToFile(report);
            }
        }

        public static TestStopwatch BeginTimeMonitor(string name)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            return new TestStopwatch(name, stopwatch);
        }

        public static void EndTimeMonitor(TestStopwatch stopwatch)
        {
            stopwatch.Stopwatch.Stop();
            s_testResults.Enqueue(new TestReport(stopwatch.Name, stopwatch.Stopwatch.Elapsed));
        }

        public static IDisposable RecordScopeTime(string name)
        {
            return new DisposableTestStopwatch(BeginTimeMonitor(name));
        }

        private readonly struct DisposableTestStopwatch : IDisposable
        {
            private readonly TestStopwatch m_stopwatch;
            public DisposableTestStopwatch(TestStopwatch stopwatch)
            {
                m_stopwatch = stopwatch;
            }

            public void Dispose()
            {
                EndTimeMonitor(m_stopwatch);
            }
        }
    }

    public struct TestStopwatch
    {
        public TestStopwatch(string name, Stopwatch stopwatch)
        {
            Name = name;
            Stopwatch = stopwatch;
        }

        public Stopwatch Stopwatch { get; }
        public string Name { get; }
    }

    public struct TestReport
    {
        public TestReport(string name, TimeSpan value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public TimeSpan Value { get; }
    }

    public class TestWriter : IDisposable
    {
        private readonly Dictionary<string, StreamWriter> m_writers = new Dictionary<string, StreamWriter>();
        private readonly DirectoryInfo m_testFolder;

        public TestWriter(DirectoryInfo rootFolderPath, string testFolderName)
        {
            if (!rootFolderPath.Exists)
                rootFolderPath.Create();

            int count = 0;

            string GetFullPath()
            {
                if (count == 0)
                    return Path.Combine(rootFolderPath.FullName, testFolderName);
                else
                    return Path.Combine(rootFolderPath.FullName, testFolderName + "_" + count);
            }

            while (Directory.Exists(GetFullPath()))
            {
                count++;
            }

            m_testFolder = new DirectoryInfo(GetFullPath());
            m_testFolder.Create();
            Debug.Log($"Tests writing to {m_testFolder.FullName}");
        }

        public void WriteToFile(TestReport report)
        {
            StreamWriter writer = m_writers.GetValueOrDefault(report.Name);
            if (writer == null)
            {
                writer = new StreamWriter(Path.Combine(m_testFolder.FullName, report.Name + ".csvpart"))
                {
                    AutoFlush = true,
                };
                m_writers[report.Name] = writer;
                writer.WriteLine($"Frame,{report.Name}");
            }

            writer.WriteLine($"{Time.frameCount},{report.Value:c}");
        }

        public void Dispose()
        {
            foreach (StreamWriter writer in m_writers.Values)
            {
                writer.Flush();
                writer.Dispose();
            }
        }
    }
}