using System.Collections.Generic;
using UnityEngine;

namespace SFramework.Repositories.Runtime
{
    public static partial class SFExtensions
    {
        public static void FindAllPaths(this ISFNode[] nodes, out List<string> paths, int targetLayer = -1)
        {
            var ids = new HashSet<string>();
            paths = new List<string>();

            foreach (var root in nodes)
            {
                var childPaths = GetChildPaths(root, "");

                if (childPaths == null) continue;

                foreach (var path in childPaths)
                {
                    ids.Add(path);
                }
            }


            foreach (var id in ids)
            {
                var split = id.Split("/");
                var path = string.Empty;
                var childLevel = split.Length;

                if (targetLayer > -1)
                {
                    if (childLevel < targetLayer) continue;
                    childLevel = Mathf.Clamp(childLevel, 0, targetLayer);
                }
                
                for (var i = 0; i < childLevel; i++)
                {
                    path += split[i];
                    if (i < childLevel - 1)
                    {
                        path += "/";
                    }
                }

                if (string.IsNullOrWhiteSpace(path)) continue;

                paths.Add(path);
            }
        }

        private static List<string> GetChildPaths(ISFNode node, string path)
        {
            var paths = new List<string>();

            path += node._Name;

            if (node.Nodes == null)
            {
                paths.Add(path);
                return paths;
            }

            if (node.Nodes.Length == 0)
            {
                paths.Add(path);
                return paths;
            }

            foreach (var child in node.Nodes)
            {
                var childPaths = GetChildPaths(child, path + "/");
                if (childPaths == null) continue;
                paths.AddRange(childPaths);
            }

            return paths;
        }
    }
}