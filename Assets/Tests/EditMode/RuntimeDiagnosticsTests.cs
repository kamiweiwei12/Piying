using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YingYun.Rhythm.Unity.Diagnostics;

namespace YingYun.Rhythm.Tests
{
    public sealed class RuntimeDiagnosticsTests
    {
        [Test]
        public void ShareableLog_ContainsEnvironmentHeaderAndCapturedMessage()
        {
            const string marker = "M10 diagnostic test marker";
            var gameObject = new GameObject("RuntimeDiagnosticsTests");
            RuntimeDiagnostics diagnostics = gameObject.AddComponent<RuntimeDiagnostics>();
            diagnostics.Initialize();

            try
            {
                LogAssert.Expect(LogType.Log, marker);
                Debug.Log(marker);

                string content = diagnostics.GetShareableText();
                Assert.That(content, Does.Contain("=== 影韵 Demo 运行诊断 ==="));
                Assert.That(content, Does.Contain("操作系统:"));
                Assert.That(content, Does.Contain(marker));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
