using SFramework.Configs.Runtime;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.U2D;

namespace SFramework.Configs.Editor
{
    [CustomPropertyDrawer(typeof(SFAssetAttribute))]
    public class SFAssetAttributeDrawer : PropertyDrawer
    {
        private static AddressableAssetSettings _settings;

        private static AddressableAssetSettings Settings
        {
            get
            {
                if (_settings == null) _settings = AddressableAssetSettingsDefaultObject.Settings;
                return _settings;
            }
        }

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

            var statusButtonWidth = 20f;
            var labelWidth = EditorGUIUtility.labelWidth;

            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var statusButtonRect = new Rect(position.x + labelWidth, position.y, statusButtonWidth, position.height);

            var hasSubAsset = HasSubAssetInfo(storedPathOrAddress);
            var subFieldWidth = hasSubAsset ? 80f : 0f;
            var fieldSpacing = hasSubAsset ? 4f : 0f;

            var fieldRect = new Rect(
                position.x + labelWidth + statusButtonWidth + 2f,
                position.y,
                position.width - labelWidth - statusButtonWidth - 2f - subFieldWidth - fieldSpacing,
                position.height
            );

            var subAssetRect = new Rect(
                fieldRect.xMax + fieldSpacing,
                position.y,
                subFieldWidth,
                position.height
            );

            // Draw Label
            EditorGUI.LabelField(labelRect, label);

            // Draw status button (readonly A/D)
            GUI.enabled = false;
            GUI.Button(statusButtonRect, GetSourceTypeLetter(storedPathOrAddress));
            GUI.enabled = true;

            if (loadedAsset != null || string.IsNullOrEmpty(storedPathOrAddress))
            {
                EditorGUI.BeginChangeCheck();
                var newAsset = EditorGUI.ObjectField(fieldRect, loadedAsset, filterType, false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newAsset != null)
                    {
                        property.stringValue = GetAddressableOrPath(newAsset);
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

            if (hasSubAsset && loadedAsset != null)
            {
                DrawSubAssetField(subAssetRect, storedPathOrAddress);
            }

            EditorGUI.EndProperty();
        }


        private string GetSourceTypeLetter(string pathOrAddress)
        {
            if (string.IsNullOrEmpty(pathOrAddress))
                return "-";
            
            var bracketIndex = pathOrAddress.IndexOf('[');
            var mainPath = bracketIndex >= 0 ? pathOrAddress.Substring(0, bracketIndex) : pathOrAddress;
            var guid = AssetDatabase.AssetPathToGUID(mainPath);
            if (string.IsNullOrEmpty(guid)) return "D";
            var entry = Settings?.FindAssetEntry(guid);
            return entry != null ? "A" : "D";
        }

        private void DrawInvalidPathField(SerializedProperty property, Rect fieldRect, string currentPath)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            property.stringValue = EditorGUI.TextField(fieldRect, currentPath);
            GUI.backgroundColor = originalColor;
        }

        private bool HasSubAssetInfo(string pathOrAddress)
        {
            if (string.IsNullOrEmpty(pathOrAddress))
                return false;

            return pathOrAddress.Contains(".spriteatlas[") ||
                   pathOrAddress.Contains(".spriteatlasv2[") ||
                   pathOrAddress.Contains(".mixer[");
        }

        private string ExtractSubAssetName(string pathOrAddress)
        {
            if (string.IsNullOrEmpty(pathOrAddress))
                return string.Empty;

            var bracketIndex = pathOrAddress.IndexOf('[');
            if (bracketIndex >= 0 && pathOrAddress.EndsWith("]"))
            {
                return pathOrAddress.Substring(bracketIndex + 1, pathOrAddress.Length - bracketIndex - 2);
            }

            return string.Empty;
        }

        private void DrawSubAssetField(Rect rect, string pathOrAddress)
        {
            var parentAsset = LoadParentAsset(pathOrAddress);
            if (parentAsset == null)
                return;

            if (GUI.Button(rect, "Root"))
            {
                EditorGUIUtility.PingObject(parentAsset);
            }
        }

        private Object LoadParentAsset(string pathOrAddress)
        {
            if (string.IsNullOrEmpty(pathOrAddress))
                return null;

            var bracketIndex = pathOrAddress.IndexOf('[');
            if (bracketIndex < 0)
                return null;

            var mainPath = pathOrAddress.Substring(0, bracketIndex);

            if (mainPath.EndsWith(".spriteatlasv2") || mainPath.EndsWith(".spriteatlas"))
            {
                return AssetDatabase.LoadAssetAtPath<SpriteAtlas>(mainPath);
            }
            else if (mainPath.EndsWith(".mixer"))
            {
                return AssetDatabase.LoadAssetAtPath<AudioMixer>(mainPath);
            }

            return null;
        }

        private Object LoadAsset(string pathOrAddress, System.Type filterType)
        {
            if (string.IsNullOrEmpty(pathOrAddress))
                return null;

            // Handle AudioMixerGroup or Snapshot from mixer
            if ((filterType == typeof(AudioMixerGroup) || filterType == typeof(AudioMixerSnapshot)) &&
                pathOrAddress.Contains(".mixer["))
            {
                var bracketIndex = pathOrAddress.IndexOf('[');
                var mixerPath = pathOrAddress.Substring(0, bracketIndex);
                var targetName = pathOrAddress.Substring(bracketIndex + 1, pathOrAddress.Length - bracketIndex - 2);

                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
                if (mixer != null)
                {
                    if (filterType == typeof(AudioMixerGroup))
                    {
                        var groups = mixer.FindMatchingGroups(targetName);
                        if (groups != null && groups.Length > 0)
                            return groups[0];
                    }
                    else if (filterType == typeof(AudioMixerSnapshot))
                    {
                        return mixer.FindSnapshot(targetName);
                    }
                }

                return null;
            }

            // Handle Sprite from Atlas
            if (filterType == typeof(Sprite) &&
                (pathOrAddress.Contains(".spriteatlasv2[") || pathOrAddress.Contains(".spriteatlas[")))
            {
                var bracketIndex = pathOrAddress.IndexOf('[');
                var atlasPath = pathOrAddress.Substring(0, bracketIndex);
                var spriteName = pathOrAddress.Substring(bracketIndex + 1, pathOrAddress.Length - bracketIndex - 2);

                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas != null)
                {
                    var sprite = atlas.GetSprite(spriteName);
                    if (sprite != null)
                    {
                        sprite.name = sprite.name.Replace("(Clone)", "");
                        return sprite;
                    }

                    // fallback: manually load subassets
                    var allObjects = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
                    foreach (var obj in allObjects)
                    {
                        if (obj is Sprite s && s.name == spriteName)
                            return s;
                    }
                }

                return null;
            }

            // Normal direct asset load
            var asset = AssetDatabase.LoadAssetAtPath(pathOrAddress, filterType);
            if (asset != null)
                return asset;

            // Try Addressables
            var allGuids = AssetDatabase.FindAssets($"t:{filterType.Name}");
            foreach (var guid in allGuids)
            {
                var entry = Settings?.FindAssetEntry(guid);
                if (entry != null && entry.address == pathOrAddress)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    return AssetDatabase.LoadAssetAtPath(assetPath, filterType);
                }
            }

            return null;
        }

        private string GetAddressableOrPath(Object asset)
        {
            if (asset is Sprite sprite)
            {
                var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
                foreach (var guid in atlasGuids)
                {
                    var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                    var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                    if (atlas != null && atlas.CanBindTo(sprite))
                    {
                        return $"{atlasPath}[{sprite.name}]";
                    }
                }

                return GetAddressableOrRegularPath(sprite);
            }

            if (asset is AudioMixerGroup group)
            {
                var mixerPath = AssetDatabase.GetAssetPath(group.audioMixer);
                return $"{mixerPath}[{group.name}]";
            }

            if (asset is AudioMixerSnapshot snapshot)
            {
                var mixerPath = AssetDatabase.GetAssetPath(snapshot.audioMixer);
                return $"{mixerPath}[{snapshot.name}]";
            }

            return GetAddressableOrRegularPath(asset);
        }

        private string GetAddressableOrRegularPath(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = Settings?.FindAssetEntry(guid);
            return entry != null ? entry.address : path;
        }
    }
}