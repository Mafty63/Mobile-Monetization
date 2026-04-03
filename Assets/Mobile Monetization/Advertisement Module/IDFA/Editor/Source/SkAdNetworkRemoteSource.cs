using System.IO;
using System.Net;
using UnityEngine;

namespace MobileCore.Advertisement.IosSupport.Editor
{
    internal class SkAdNetworkRemoteSource : ISkAdNetworkSource
    {
        public string Path { get; }

        public SkAdNetworkRemoteSource(string path)
        {
            Path = path;
        }

        public Stream Open()
        {
            return new WebClient().OpenRead(Path);
        }
    }
}
