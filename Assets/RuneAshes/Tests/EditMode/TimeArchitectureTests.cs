using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class TimeArchitectureTests
    {
        /// <summary>
        /// 架构守卫：业务代码不得直接写 Time.timeScale，唯一允许的是 UnityTime.cs。
        /// 注释里出现 "Time.timeScale" 不算，必须匹配赋值符号。
        /// </summary>
        [Test]
        public void BusinessCode_DoesNotWrite_TimeScaleDirectly()
        {
            var root = Path.Combine(Application.dataPath, "RuneAshes");
            var pattern = new Regex(@"Time\.timeScale\s*=");
            var offenders = new List<string>();

            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.EndsWith("UnityTime.cs"))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (pattern.IsMatch(text))
                {
                    offenders.Add(file.Replace(Application.dataPath, "Assets"));
                }
            }

            Assert.IsEmpty(offenders,
                "以下文件直接写了 Time.timeScale，请改用 GameTime.Current / ITime：\n" +
                string.Join("\n", offenders));
        }
    }
}
