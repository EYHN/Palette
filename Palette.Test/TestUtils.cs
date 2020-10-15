using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Palette.Test
{
    public static class TestUtils
    {
        public static string GetResourceFullPath(string name)
        {
            return Path.Join(GetApplicationRoot(), "Resources", name);
        }

        public static Stream ReadResourceStream(string name)
        {
            return new FileStream(GetResourceFullPath(name), FileMode.Open, FileAccess.Read);
        }

        public static byte[] ReadResource(string name)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Stream ReadStream = ReadResourceStream(name))
                {
                    ReadStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        public static async Task SaveResult(string Name, Stream Data, TestContext TestContext)
        {
            var ClassName = Regex.Replace(TestContext.FullyQualifiedTestClassName, ".*\\.", "");
            Directory.CreateDirectory(Path.Join(TestContext.TestResultsDirectory, ClassName));
            var FileName = Path.Join(TestContext.TestResultsDirectory, ClassName, Name);
            using (Stream output = new FileStream(FileName, FileMode.OpenOrCreate))
            {
                await Data.CopyToAsync(output);
            }
            TestContext.AddResultFile(FileName);
        }

        public static string GetApplicationRoot()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
    }
}
