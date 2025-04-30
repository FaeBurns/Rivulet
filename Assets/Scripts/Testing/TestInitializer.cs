using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Testing
{
    public class TestInitializer : MonoBehaviour
    {
        public string RootDirectoryName;
        public string TestFolderName;

        private void Start()
        {
            Tester.Init(new DirectoryInfo(RootDirectoryName), TestFolderName);
        }

        private void Update()
        {
            Tester.Flush();
        }
    }
}