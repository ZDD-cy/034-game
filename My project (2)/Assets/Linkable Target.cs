using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LinkableTarget : MonoBehaviour
{
    [Header("目标大小参数")]
    public float targetSize = 1f; // 你要的比例参数
}
