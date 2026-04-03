using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisement.IosSupport.Editor
{
    internal interface ISkAdNetworkParser
    {
        string GetExtension();
        HashSet<string> ParseSource(ISkAdNetworkSource source);
    }
}
