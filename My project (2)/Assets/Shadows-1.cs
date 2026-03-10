using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.Collections.Generic;

public static class ShadowCaster2DTilemapGenerator
{
    [MenuItem("Tools/Generate ShadowCaster2D from Tilemap")]
    static void Generate()
    {
        Debug.Log("===== 开始生成 ShadowCaster2D =====");

        Tilemap[] tilemaps = GameObject.FindObjectsOfType<Tilemap>();
        if (tilemaps.Length == 0)
        {
            Debug.LogWarning("场景中没有找到 Tilemap。");
            return;
        }

        foreach (Tilemap tilemap in tilemaps)
        {
            Debug.Log("检查 Tilemap: " + tilemap.name);

            CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                Debug.LogWarning("Tilemap " + tilemap.name + " 没有 CompositeCollider2D，跳过。");
                continue;
            }

            Debug.Log("CompositeCollider2D 的 GeometryType: " + composite.geometryType);
            Debug.Log("CompositeCollider2D 的 pathCount = " + composite.pathCount);

            if (composite.pathCount == 0)
            {
                Debug.LogWarning("Tilemap " + tilemap.name + " 的 CompositeCollider2D 路径数量为 0，请检查碰撞体设置。");
                continue;
            }

            // 安全删除之前生成的 ShadowCaster2D 子物体
            ShadowCaster2D[] oldCasters = tilemap.GetComponentsInChildren<ShadowCaster2D>();
            if (oldCasters.Length > 0)
            {
                Debug.Log("正在删除 " + oldCasters.Length + " 个旧的 ShadowCaster2D...");
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (var caster in oldCasters)
                {
                    toDestroy.Add(caster.gameObject);
                }
                foreach (var go in toDestroy)
                {
                    GameObject.DestroyImmediate(go);
                }
            }

            Vector3 tilemapWorldPos = tilemap.transform.position; // 用于坐标转换

            for (int i = 0; i < composite.pathCount; i++)
            {
                int pointCount = composite.GetPathPointCount(i);
                Vector2[] points2D = new Vector2[pointCount];
                composite.GetPath(i, points2D);

                Debug.Log($"路径 {i}: 点数 = {pointCount}");

                if (pointCount < 3)
                {
                    Debug.LogWarning($"路径 {i} 的点数少于 3，无法形成有效多边形，跳过。");
                    continue;
                }

                // 打印第一个点调试
                if (points2D.Length > 0)
                {
                    Debug.Log($"路径 {i} 第一个点 (世界坐标): ({points2D[0].x}, {points2D[0].y})");
                }

                // 将世界坐标转换为相对于 Tilemap 的局部坐标
                Vector3[] points3D = new Vector3[pointCount];
                for (int j = 0; j < pointCount; j++)
                {
                    points3D[j] = new Vector3(
                        points2D[j].x - tilemapWorldPos.x,
                        points2D[j].y - tilemapWorldPos.y,
                        0f);
                }

                // 创建子物体
                GameObject go = new GameObject("ShadowCaster2D_" + i);
                go.transform.parent = tilemap.transform;
                go.transform.localPosition = Vector3.zero; // 子物体位于 Tilemap 原点，轮廓通过路径定义
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                ShadowCaster2D caster = go.AddComponent<ShadowCaster2D>();
                bool success = SetShadowCasterPath(caster, points3D);

                if (success)
                {
                    // 强制刷新组件
                    caster.enabled = false;
                    caster.enabled = true;
                    Debug.Log($"路径 {i} 设置成功。");
                }
                else
                {
                    Debug.LogError($"路径 {i} 设置失败。");
                }
            }
        }

        Debug.Log("===== ShadowCaster2D 生成完成 =====");
    }

    static bool SetShadowCasterPath(ShadowCaster2D caster, Vector3[] points)
    {
        System.Type type = caster.GetType();
        System.Reflection.FieldInfo field = type.GetField("m_ShapePath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(caster, points);
            return true;
        }
        else
        {
            Debug.LogError("无法获取 ShadowCaster2D 的 m_Shap