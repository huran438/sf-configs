using SFramework.Configs.Runtime;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D;

namespace SFramework.Configs.Editor
{
    [CustomPropertyDrawer(typeof(SFAssetAttribute), true)]
    public class SFAssetAttributeDrawer : PropertyDrawer
    {
        private static AddressableAssetSettings _settings;

        private static AddressableAssetSettings Settings =>
            _settings ??= AddressableAssetSettingsDefaultObject.Settings;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [SFAsset] with string fields only.");
                return;
            }

            var assetReferencePath = (SFAssetAttribute)attribute;
            var filterType = assetReferencePath.AssetType ?? typeof(Object);

            EditorGUI.BeginProperty(position, label, property);

            var storedPathOrAddress = property.stringValue;
            var loadedAsset = LoadAsset(storedPathOrAddress, filterType);

            var labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            if (loadedAsset != null || string.IsNullOrEmpty(storedPathOrAddress))
            {
                EditorGUI.BeginChangeCheck();
                var newAsset = EditorGUI.ObjectField(fieldRect, loadedAsset, filterType, false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newAsset != null)
                    {
                        var newPath = GetAddressableOrPath(newAsset);
                        if (!string.IsNullOrEmpty(newPath))
                        {
                            property.stringValue = newPath;
                        }
                        else
                        {
                            Debug.LogWarning($"Asset '{newAsset.name}' is not addressable. Only Addressables are allowed.");
                        }
                    }
                    else
                    {
                        property.stringValue = string.Empty;
                    }
                }
            }
            else
            {
                DrawInvalidPathField(property, fieldRect, storedPathOrAddress);
            }

            EditorGUI.EndProperty();
        }

        private void DrawInvalidPathField(SerializedProperty property, Rect fieldRect, string currentPath)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            property.stringValue = EditorGUI.TextField(fieldRect, currentPath);
            GUI.backgroundColor = originalColor;
        }

        private Object LoadAsset(string pathOrAddress, System.Type filterType)
        {
            if (string.IsNullOrEmpty(pathOrAddress)) return null;

            // Sprite from Atlas
            if (filterType == typeof(Sprite) &&
                (pathOrAddress.Contains(".spriteatlasv2[") || pathOrAddress.Contains(".spriteatlas[")))
            {
                var atlasPath = pathOrAddress[..pathOrAddress.IndexOf('[')];
                var spriteName = pathOrAddress[(pathOrAddress.IndexOf('[') + 1)..^1];

                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null) return null;
                var sprite = atlas.GetSprite(spriteName);
                if (sprite != null) return sprite;

                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(atlasPath))
                {
                    if (obj is Sprite s && s.name == spriteName)
                        return s;
                }

                return null;
            }

            // Direct asset load
            var asset = AssetDatabase.LoadAssetAtPath(pathOrAddress, filterType);
            if (asset != null) return asset;

            // Addressables lookup by address
            var guids = AssetDatabase.FindAssets($"t:{filterType.Name}");
            foreach (var guid in guids)
            {
                var entry = Settings?.FindAssetEntry(guid);
                if (entry == null || entry.address != pathOrAddress) continue;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath(assetPath, filterType);
            }

            return null;
        }

        private string GetAddressableOrPath(Object asset)
        {
            if (asset == null) return null;

            // Sprite from Atlas
            if (asset is Sprite sprite)
            {
                var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
                foreach (var guid in atlasGuids)
                {
                    var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                    var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                    if (atlas == null || !atlas.CanBindTo(sprite)) continue;
                    var atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
                    var atlasEntry = Settings?.FindAssetEntry(atlasGuid);
                    return atlasEntry != null ? $"{atlasPath}[{sprite.name}]" : null;
                }

                return null;
            }

            // General addressable
            var path = AssetDatabase.GetAssetPath(asset);
            var entry = Settings?.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            return entry != null ? entry.address : null;
        }
    }
}