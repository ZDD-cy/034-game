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
        // 获取场景中所有 Tilemap
        Tilemap[] tilemaps = GameObject.FindObjectsOfType<Tilemap>();
        if (tilemaps.Length == 0)
        {
            Debug.LogWarning("场景中没有找到 Tilemap。");
            return;
        }

        foreach (Tilemap tilemap in tilemaps)
        {
            // 尝试获取 CompositeCollider2D
            CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                Debug.LogWarning("Tilemap " + tilemap.name + " 没有 CompositeCollider2D，跳过。");
                continue;
            }

            // 清除之前生成的 ShadowCaster2D（可选）
            ShadowCaster2D[] oldCasters = tilemap.GetComponents<ShadowCaster2D>();
            foreach (var caster in oldCasters)
            {
                GameObject.DestroyImmediate(caster);
            }

            // 获取碰撞体的路径并为每条路径创建 ShadowCaster2D
            for (int i = 0; i < composite.pathCount; i++)
            {
                Vector2[] points = new Vector2[composite.GetPathPointCount(i)];
                composite.GetPath(i, points);

                // 创建 GameObject 并添加 ShadowCaster2D
                GameObject go = new GameObject("ShadowCaster2D_" + i);
                go.transform.parent = tilemap.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                ShadowCaster2D caster = go.AddComponent<ShadowCaster2D>();
                SetShadowCasterPath(caster, points);
            }
        }

        Debug.Log("ShadowCaster2D 生成完成！");
    }

    static void SetShadowCasterPath(ShadowCaster2D caster, Vector2[] points)
    {
        // 利用反射设置私有字段 m_ShapePath
        System.Type type = caster.GetType();
        System.Reflection.FieldInfo field = type.GetField("m_ShapePath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(caster, points);
        }
        else
        {
            Debug.LogError("无法设置 ShadowCaster2D 的路径，请检查 Unity 版本。");
        }

        // Unity 2022.3 中没有 ForceUpdate，但设置路径后阴影会自动更新
    }
}