﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.HLODSystem.Simplifier;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    static class HLODCreator
    {
        public static HLOD Setup(GameObject root)
        {
            if (root.GetComponent<HLOD>() != null)
            {
                Debug.LogWarning("It has already been set.");
                return null;
            }

            HLOD hlod = root.AddComponent<HLOD>();
            return hlod;           
        }

        private static List<MeshRenderer> GetMeshRenderers(List<GameObject> gameObjects)
        {
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

            for (int i = 0; i < gameObjects.Count; ++i)
            {
                GameObject obj = gameObjects[i];
                LODGroup lodGroup = obj.GetComponent<LODGroup>();

                Renderer[] renderers;

                if (lodGroup != null)
                {
                    renderers = lodGroup.GetLODs().Last().renderers;
                }
                else
                {
                    renderers = obj.GetComponents<Renderer>();
                }

                for (int ri = 0; ri < renderers.Length; ++ri)
                {
                    MeshRenderer mr = renderers[ri] as MeshRenderer;
                    if ( mr != null )
                        meshRenderers.Add(mr);
                }
            }

            return meshRenderers;
        }

        private static List<HLODBuildInfo> CreateBuildInfo(SpaceNode root)
        {
            List<HLODBuildInfo> results = new List<HLODBuildInfo>();
            Stack<SpaceNode> trevelStack = new Stack<SpaceNode>();
            Stack<int> parentStack = new Stack<int>();

            trevelStack.Push(root);
            parentStack.Push(-1);
            

            while (trevelStack.Count > 0)
            {
                int currentNodeIndex = results.Count;
                SpaceNode node = trevelStack.Pop();
                HLODBuildInfo info = new HLODBuildInfo
                {
                    parentIndex = parentStack.Pop(),
                    target = node
                };

                if (node.ChildTreeNodes != null)
                {
                    for (int i = 0; i < node.ChildTreeNodes.Count; ++i)
                    {
                        trevelStack.Push(node.ChildTreeNodes[i]);
                        parentStack.Push(currentNodeIndex);
                    }
                }

                results.Add(info);

                //it should add to every parent.
                List<MeshRenderer> meshRenderers = GetMeshRenderers(node.Objects);
                int distance = 0;

                while (currentNodeIndex >= 0)
                {
                    var curInfo = results[currentNodeIndex];
                    
                    curInfo.renderers.AddRange(meshRenderers);
                    curInfo.distances.AddRange(Enumerable.Repeat(distance,meshRenderers.Count));

                    currentNodeIndex = curInfo.parentIndex;
                    distance += 1;
                }

            }

            return results;
        }

        public static IEnumerator CreateWithoutPrefab(HLOD hlod)
        {
            Stopwatch sw = new Stopwatch();

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            sw.Reset();
            sw.Start();

            hlod.CalcBounds();

            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(hlod.gameObject);
            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(5.0f, hlod.MinSize);
            SpaceNode rootNode = spliter.CreateSpaceTree(hlod.Bounds, hlodTargets);

            List<HLODBuildInfo> buildInfos = CreateBuildInfo(rootNode);           
            
            Debug.Log("[HLOD] Splite space: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();
            /*            
            ISimplifier simplifier = (ISimplifier)Activator.CreateInstance(hlod.SimplifierType, new object[]{hlod});
            if ( simplifier == null )
                yield break;

            for (int i = 0; i < buildInfos.Count; ++i)
            {
                yield return new BranchCoroutine(simplifier.Simplify(buildInfos[i]));
            }

            yield return new WaitForBranches();
            Debug.Log("[HLOD] Simplify: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            IBatcher batcher = (IBatcher)Activator.CreateInstance(hlod.BatcherType);
            if ( batcher == null )
                yield break;

            //batcher.Batch(targetHlods.Last(), targetHlods.Select(h => h.LowRoot).ToArray());
            Debug.Log("[HLOD] Batch: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();
            
            AssetDatabase.StartAssetEditing();
            IStreamingBuilder builder = (IStreamingBuilder)Activator.CreateInstance(hlod.StreamingType);
            if ( builder == null )
                yield break;
            //for (int i = 0; i < targetHlods.Count; ++i)
            //{
            //    IStreamingBuilder builder = (IStreamingBuilder)Activator.CreateInstance(targetHlods[i].StreamingType);
            //    builder.Build(targetHlods[i], targetHlods[i] == hlod);
            //}
            Debug.Log("[HLOD] Build: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            AssetDatabase.StopAssetEditing();
            Debug.Log("[HLOD] Importing: " + sw.Elapsed.ToString("g"));
            //combine
            
            //storing
            */

            yield break;
            
        }
        public static IEnumerator Create(HLOD hlod)
        {
            Stopwatch sw = new Stopwatch();
            List<HLOD> targetHlods = null;
           
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            sw.Reset();
            sw.Start();
            hlod.CalcBounds();
            //if (hlod.RecursiveGeneration == true)
            //{
            //    if (hlod.Bounds.size.x > hlod.MinSize)
            //    {
            //        ISplitter splitter = new OctSplitter();
            //        splitter.Split(hlod);
            //    }

            //    //GetComponentsInChildren is not working.
            //    //so, I made it manually.
            //    targetHlods = ObjectUtils.GetComponentsInChildren<HLOD>(hlod.gameObject);
            //}
            //else
            //{
            //    targetHlods = new List<HLOD>();
            //    targetHlods.Add(hlod);
            //}
           
            for (int i = 0; i < targetHlods.Count; ++i)
            {
                var curHlod = targetHlods[i];
                curHlod.HighRoot = CreateHigh(curHlod.gameObject);
                curHlod.LowRoot = CreateLow(curHlod, curHlod.HighRoot);

                curHlod.HighRoot.transform.SetParent(curHlod.transform);
                curHlod.LowRoot.transform.SetParent(curHlod.transform);
            }
            Debug.Log("[HLOD] Splite space: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                ISimplifier simplifier = (ISimplifier)Activator.CreateInstance(targetHlods[i].SimplifierType);
                yield return new BranchCoroutine(simplifier.Simplify(targetHlods[i]));
            }

            yield return new WaitForBranches();
            Debug.Log("[HLOD] Simplify: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            IBatcher batcher = (IBatcher)Activator.CreateInstance(hlod.BatcherType);
            batcher.Batch(targetHlods.Last(), targetHlods.Select(h => h.LowRoot).ToArray());
            Debug.Log("[HLOD] Batch: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();


            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < targetHlods.Count; ++i)
            {
                IStreamingBuilder builder = (IStreamingBuilder)Activator.CreateInstance(targetHlods[i].StreamingType);
                builder.Build(targetHlods[i], targetHlods[i] == hlod);
            }
            Debug.Log("[HLOD] Build: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            AssetDatabase.StopAssetEditing();
            Debug.Log("[HLOD] Importing: " + sw.Elapsed.ToString("g"));
            
            
            //for (int i = 0; i < targetHlods.Count; ++i)
            //{
            //    PrefabUtils.SavePrefab(targetHlods[i]);
            //}
            //Debug.Log("[HLOD] SavePrefab: " + sw.Elapsed.ToString("g"));
            
        }

        public static IEnumerator Update(HLOD hlod)
        {
            yield return Destroy(hlod);
            yield return Create(hlod);

        }

        public static IEnumerator Destroy(HLOD hlod)
        {
            List<HLOD> targetHlods = ObjectUtils.GetComponentsInChildren<HLOD>(hlod.gameObject).ToList();

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                GameObject obj = PrefabUtility.GetOutermostPrefabInstanceRoot(targetHlods[i].gameObject);
                if (obj == null)
                    continue;

                PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                List<GameObject> hlodTargets = ObjectUtils.HLODTargets(targetHlods[i].HighRoot);

                for (int ti = 0; ti < hlodTargets.Count; ++ti)
                {
                    ObjectUtils.HierarchyMove(hlodTargets[ti], targetHlods[i].HighRoot, hlod.gameObject );
                }

                if ( targetHlods[i] != hlod )
                    Object.DestroyImmediate(targetHlods[i].gameObject);
            }

            Object.DestroyImmediate(hlod.HighRoot);
            Object.DestroyImmediate(hlod.LowRoot);

            yield break;
            
        }

        static GameObject CreateHigh(GameObject root)
        {
            GameObject high = new GameObject("High");

            while (root.transform.childCount > 0)
            {
                Transform child = root.transform.GetChild(0);
                child.SetParent(high.transform);
            }

            return high;
        }

        static GameObject CreateLow(HLOD hlod, GameObject highGameObject)
        {
            GameObject low = new GameObject("Low");

            List<Renderer> renderers = new List<Renderer>();

            //Convert gameobject to MeshRenderer.
            //This gameObjects are mixed LODGroup and MeshRenderer.
            List<GameObject> gameObjects = ObjectUtils.HLODTargets(highGameObject);
            for (int i = 0; i < gameObjects.Count; ++i)
            {
                var lodGroup = gameObjects[i].GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    renderers.AddRange(lodGroup.GetLODs().Last().renderers);
                    continue;
                }

                var renderer = gameObjects[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderers.Add(renderer);
                }
            }

            for (int i = 0; i < renderers.Count; ++i)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                float max = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                if (max < hlod.ThresholdSize)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                GameObject rendererObject = new GameObject(renderers[i].name, typeof(MeshFilter), typeof(MeshRenderer), typeof(LowMeshHolder));

                EditorUtility.CopySerialized(filter, rendererObject.GetComponent<MeshFilter>());
                EditorUtility.CopySerialized(renderer, rendererObject.GetComponent<MeshRenderer>());
                var holder = rendererObject.AddComponent<Utils.SimplificationDistanceHolder>();
                holder.OriginGameObject = renderer.gameObject;

                rendererObject.transform.SetParent(low.transform);
                rendererObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                rendererObject.transform.localScale = renderer.transform.lossyScale;
            }

            
            return low;
        }

    }
}
