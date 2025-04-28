using UnityEngine;

namespace SFramework.Configs.Runtime
{
    public class SFAssetAttribute : PropertyAttribute
    {
        public System.Type AssetType { get; private set; }

        public SFAssetAttribute(System.Type assetType = null)
        {
            AssetType = assetType ?? typeof(Object);
        }
    }
}