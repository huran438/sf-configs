using SFramework.Configs.Runtime;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace SFramework.Configs.Editor
{
    [CustomPropertyDrawer(typeof(SFAssetAttribute), true)]
    public class SFAssetAttributeDrawer : PropertyDrawer
    {
        private static bool AddressablesAvailable =>
            Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor") != null;

        private static object GetAddressablesSettings()
        {
            var defaultObjType = Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            var settingsProp = defaultObjType?.GetProperty("Settings", BindingFlags.Public | BindingFlags.Static);
            return settingsProp?.GetValue(null, null);
        }

        private static object FindAddressableEntry(string guid)
        {
            var settings = GetAddressablesSettings();
            if (settings == null) return null;
            var t = settings.GetType();
            // Try overload with includeHidden parameter first
            var mWithHidden = t.GetMethod("FindAssetEntry", new[] { typeof(string), typeof(bool) });
            if (mWithHidden != null)
                return mWithHidden.Invoke(settings, new object[] { guid, true });
            var m = t.GetMethod("FindAssetEntry", new[] { typeof(string) });
            return m != null ? m.Invoke(settings, new object[] { guid }) : null;
        }

        private static string GetAddressFromEntry(object entry)
        {
            if (entry == null) return null;
            var p = entry.GetType().GetProperty("address", BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(entry) as string;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [SFAsset] with string fields only.");
                return;
            }

            var assetReferencePath = (SFAssetAttribute)attribute;
            var filterType = assetReferencePath.AssetType ?? typeof(UnityEngine.Object);

            EditorGUI.BeginProperty(position, label, property);

            var storedPathOrAddress = property.stringValue;
            var loadedAsset = LoadAsset(storedPathOrAddress, filterType);
            var isInvalidStored = !string.IsNullOrEmpty(storedPathOrAddress) && loadedAsset == null;

            var labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);
            if (isInvalidStored)
            {
                var tinted = new Color(1f, 0f, 0f, 0.15f);
                EditorGUI.DrawRect(fieldRect, tinted);
            }

            EditorGUI.BeginChangeCheck();
            var newAsset = EditorGUI.ObjectField(fieldRect, loadedAsset, filterType, false);
            if (EditorGUI.EndChangeCheck())
            {
                if (newAsset != null)
                {
                    var newPath = GetStorablePath(newAsset);
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        property.stringValue = newPath;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Asset '{newAsset.name}' is neither Addressable nor under a Resources folder. Please place it under a Resources folder or make it Addressable.");
                    }
                }
                else
                {
                    property.stringValue = string.Empty;
                }
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

        private UnityEngine.Object LoadAsset(string pathOrAddress, System.Type filterType)
        {
            if (string.IsNullOrEmpty(pathOrAddress)) return null;

            // Sprite with bracket notation [SpriteName]. Left key can be:
            // - Addressables address
            // - Assets path
            // - Resources-relative path (no extension)
            if (filterType == typeof(Sprite) && pathOrAddress.Contains("[") && pathOrAddress.EndsWith("]"))
            {
                var atlasKey = pathOrAddress[..pathOrAddress.IndexOf('[')];
                var spriteName = pathOrAddress[(pathOrAddress.IndexOf('[') + 1)..^1];

                SpriteAtlas atlas = null;
                if (atlasKey.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasKey);
                }
                else
                {
                    // Treat as Resources-relative path; find matching atlas under any Resources folder
                    var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
                    foreach (var guid in atlasGuids)
                    {
                        var ap = AssetDatabase.GUIDToAssetPath(guid);
                        var idxRes = ap.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
                        if (idxRes < 0) continue;
                        var rel = ap.Substring(idxRes + "/Resources/".Length);
                        var relNoExt = rel.Contains('.') ? rel.Substring(0, rel.LastIndexOf('.')) : rel;
                        relNoExt = relNoExt.Replace('\\', '/');
                        if (string.Equals(relNoExt, atlasKey, StringComparison.Ordinal))
                        {
                            atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ap);
                            break;
                        }
                    }
                }

                if (atlas != null)
                {
                    var sprite = atlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;

                    var atlasPathForAll = AssetDatabase.GetAssetPath(atlas);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(atlasPathForAll))
                    {
                        if (obj is Sprite s && s.name == spriteName)
                            return s;
                    }

                    return null;
                }

                // Not an atlas key. Try interpret left part as an Addressables address, Assets path, or Resources-relative asset path
                // 1) Addressables address: find entry with matching address, then search sub-assets for sprite name
                if (AddressablesAvailable)
                {
                    var guids = AssetDatabase.FindAssets(""); // broad search, we'll filter by address
                    foreach (var guid in guids)
                    {
                        var entry = FindAddressableEntry(guid);
                        var address = GetAddressFromEntry(entry);
                        if (string.IsNullOrEmpty(address) || !string.Equals(address, atlasKey, StringComparison.Ordinal))
                            continue;
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                        {
                            if (obj is Sprite s && s.name == spriteName)
                                return s;
                        }
                        break;
                    }
                }

                // 2) Direct Assets path (texture or sprite asset)
                if (atlasKey.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(atlasKey))
                    {
                        if (obj is Sprite s && s.name == spriteName)
                            return s;
                    }
                }

                // 3) Resources-relative path (no extension)
                var nameOnly = Path.GetFileName(atlasKey);
                var candidates = AssetDatabase.FindAssets(nameOnly);
                foreach (var guid in candidates)
                {
                    var ap = AssetDatabase.GUIDToAssetPath(guid);
                    var idxRes = ap.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
                    if (idxRes < 0) continue;
                    var rel = ap.Substring(idxRes + "/Resources/".Length);
                    var relNoExt = rel.Contains('.') ? rel.Substring(0, rel.LastIndexOf('.')) : rel;
                    relNoExt = relNoExt.Replace('\\', '/');
                    if (!string.Equals(relNoExt, atlasKey, StringComparison.Ordinal)) continue;
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(ap))
                    {
                        if (obj is Sprite s && s.name == spriteName)
                            return s;
                    }
                }

                return null;
            }

            // Direct asset load
            var asset = AssetDatabase.LoadAssetAtPath(pathOrAddress, filterType);
            if (asset != null) return asset;
            // If requesting a Sprite by Assets path without bracket notation, try first sub-sprite
            if (filterType == typeof(Sprite) && pathOrAddress.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(pathOrAddress))
                {
                    if (obj is Sprite s) return s;
                }
            }

            // Addressables lookup by address (when available)
            if (AddressablesAvailable)
            {
                var guids = (filterType == null || filterType == typeof(UnityEngine.Object))
                    ? AssetDatabase.FindAssets("")
                    : AssetDatabase.FindAssets($"t:{filterType.Name}");
                foreach (var guid in guids)
                {
                    var entry = FindAddressableEntry(guid);
                    var address = GetAddressFromEntry(entry);
                    if (string.IsNullOrEmpty(address) || address != pathOrAddress) continue;
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    return AssetDatabase.LoadAssetAtPath(assetPath, filterType);
                }
            }

            // Resources-relative path lookup: pathOrAddress is expected to be relative to any Resources/ folder, no extension
            // Find an asset of the given type whose Resources-relative path matches
            var name = Path.GetFileName(pathOrAddress);
            var typeFilter = (filterType == null || filterType == typeof(UnityEngine.Object)) ? string.Empty : $" t:{filterType.Name}";
            var resourceGuids = AssetDatabase.FindAssets($"{name}{typeFilter}");
            foreach (var guid in resourceGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var idx = assetPath.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var rel = assetPath.Substring(idx + "/Resources/".Length);
                var relNoExt = rel.Contains('.') ? rel.Substring(0, rel.LastIndexOf('.')) : rel;
                relNoExt = relNoExt.Replace('\\', '/');
                if (string.Equals(relNoExt, pathOrAddress, System.StringComparison.Ordinal))
                {
                    return AssetDatabase.LoadAssetAtPath(assetPath, filterType);
                }
            }

            return null;
        }

        private string GetStorablePath(UnityEngine.Object asset)
        {
            if (asset == null) return null;

            // Sprite from Atlas (try to store atlas key if possible)
            string atlasKeyResult = null;
            if (asset is Sprite sprite)
            {
                var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
                foreach (var guid in atlasGuids)
                {
                    var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                    var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                    if (atlas == null || !atlas.CanBindTo(sprite)) continue;
                    // Prefer Resources path if atlas is under Resources/
                    var resIdx = atlasPath.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
                    if (resIdx >= 0)
                    {
                        var rel = atlasPath.Substring(resIdx + "/Resources/".Length);
                        rel = rel.Contains('.') ? rel.Substring(0, rel.LastIndexOf('.')) : rel;
                        rel = rel.Replace('\\', '/');
                        atlasKeyResult = $"{rel}[{sprite.name}]";
                        break;
                    }

                    // Fallback to Addressables path token if entry exists
                    if (AddressablesAvailable)
                    {
                        var atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
                        var atlasEntry = FindAddressableEntry(atlasGuid);
                        if (atlasEntry != null)
                        {
                            atlasKeyResult = $"{atlasPath}[{sprite.name}]";
                            break;
                        }
                    }

                    // As a last resort, store direct asset path with bracket (editor-only usability)
                    atlasKeyResult = $"{atlasPath}[{sprite.name}]";
                    break;
                }
            }

            if (!string.IsNullOrEmpty(atlasKeyResult))
                return atlasKeyResult;

            // Sprite without atlas: encode as [name] using best base key (Resources, Addressables, or Assets path)
            if (asset is Sprite spriteNoAtlas)
            {
                var texturePath = AssetDatabase.GetAssetPath(spriteNoAtlas);
                var idxResTex = texturePath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
                if (idxResTex >= 0)
                {
                    var rel = texturePath.Substring(idxResTex + "/Resources/".Length);
                    rel = rel.Contains('.') ? rel.Substring(0, rel.LastIndexOf('.')) : rel;
                    rel = rel.Replace('\\', '/');
                    return $"{rel}[{spriteNoAtlas.name}]";
                }

                if (AddressablesAvailable)
                {
                    var guid = AssetDatabase.AssetPathToGUID(texturePath);
                    var entry = FindAddressableEntry(guid);
                    var address = GetAddressFromEntry(entry);
                    if (!string.IsNullOrEmpty(address))
                        return $"{address}[{spriteNoAtlas.name}]";
                }

                return $"{texturePath}[{spriteNoAtlas.name}]";
            }

            // General assets
            var path = AssetDatabase.GetAssetPath(asset);
            // Prefer Resources-relative path if under Resources
            var idx = path.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var rel = path.Substring(idx + "/Resources/".Length);
                rel = rel.Contains('.') ? rel.Substring(0, rel.LastIndexOf('.')) : rel;
                rel = rel.Replace('\\', '/');
                return rel;
            }

            // If Addressables exist and an entry is present, use its address
            if (AddressablesAvailable)
            {
                var assetGuid = AssetDatabase.AssetPathToGUID(path);
                var entry = FindAddressableEntry(assetGuid);
                var address = GetAddressFromEntry(entry);
                if (!string.IsNullOrEmpty(address))
                    return address;
            }

            // Otherwise, fall back to direct asset path to avoid blocking assignment in the editor
            return path;
        }
    }
}