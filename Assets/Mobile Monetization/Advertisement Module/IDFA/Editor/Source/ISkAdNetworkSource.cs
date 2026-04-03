using System.IO;
using UnityEngine;

namespace MobileCore.Advertisement.IosSupport.Editor
{
    internal interface ISkAdNetworkSource
    {
        string Path { get; }
        Stream Open();
    }
}
