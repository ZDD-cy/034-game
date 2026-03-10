using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

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
                       // 安全删除旧的 ShadowCaster2D 子物体
            ShadowCaster2D[] oldCasters = tilemap.GetComponentsInChildren<ShadowCaster2D>();
            if (oldCasters.Length > 0)
            {
                Debug.Log("正在删除 " + oldCasters.Length + " 个旧的 ShadowCaster2D...");
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (var caster in oldCasters)
                    toDestroy.Add(caster.gameObject);
                foreach (var go in toDestroy)
                    GameObject.DestroyImmediate(go);
            }

            Vector3 tilemapWorldPos = tilemap.transform.position;

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

                // 转换为局部坐标
                Vector3[] points3D = new Vector3[pointCount];
                for (int j = 0; j < pointCount; j++)
                {
                    points3D[j] = new Vector3(
                        points2D[j].x - tilemapWorldPos.x,
                        points2D[j].y - tilemapWorldPos.y,
                        0f);
                }

                GameObject go = new GameObject("ShadowCaster2D_" + i);
                go.transform.parent = tilemap.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                ShadowCaster2D caster = go.AddComponent<ShadowCaster2D>();
                ForceSetShadowPath(caster, points3D);
            }
        }

        // 延迟一帧执行全局刷新（确保所有组件初始化完成）
        EditorApplication.delayCall += () => {
            ShadowCaster2D[] allCasters = GameObject.FindObjectsOfType<ShadowCaster2D>();
            foreach (var c in allCasters)
            {
                if (c != null)
                {
                    // 切换 enabled 触发 OnEnable
                    c.enabled = false;
                    c.enabled = true;

                    // 尝试调用内部更新方法
                    ForceRefreshShadowCaster(c);
                }
            }
            // 强制场景视图重绘
            SceneView.RepaintAll();
            Debug.Log("延迟刷新完成。");
        };

        Debug.Log("===== ShadowCaster2D 生成完成 =====");
    }

    static void ForceSetShadowPath(ShadowCaster2D caster, Vector3[] points)
    {
        // 方法1：通过 SerializedObject 设置路径（确保数据被标记为 dirty）
        SerializedObject so = new SerializedObject(caster);
        SerializedProperty prop = so.FindProperty("m_ShapePath");
        if (prop == null)
        {
            Debug.LogError("无法找到 SerializedProperty m_ShapePath");
            return;
        }

        prop.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).vector3Value = points[i];
        }
        so.ApplyModifiedProperties();

        // 方法2：反射设置字段（双重保险）
        System.Type type = caster.GetType();
        FieldInfo field = type.GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(caster, points);
        }

        // 方法3：强制调用内部更新方法
        ForceRefreshShadowCaster(caster);
    }

    static void ForceRefreshShadowCaster(ShadowCaster2D caster)
    {
        if (caster == null) return;

        System.Type type = caster.GetType();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // 尝试调用 UpdateGeometry（如果存在）
        MethodInfo updateGeo = type.GetMethod("UpdateGeometry", flags);
        if (updateGeo != null)
        {
            updateGeo.Invoke(caster, null);
        }
        else
        {
            // 否则尝试 OnEnable
            MethodInfo onEnable = type.GetMethod("OnEnable", flags);
            onEnable?.Invoke(caster, null);
        }

        // 切换 enabled 确保 OnEnable 被调用
        caster.enabled = false;
        caster.enabled = true;
    }
}