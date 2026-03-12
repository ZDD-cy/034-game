using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PrincessBubbleController : MonoBehaviour
{
    [Header("气泡设置")]
    public GameObject bubbleUI;          // 头顶气泡面板
    public Image circleImg;              // 圆形图标
    public Image triangleImg;           // 三角形图标
    public Image squareImg;             // 方块图标
    public Color inactiveColor = Color.gray;  // 未激活颜色（灰色）
    public Color activeColor = Color.green;   // 激活颜色（绿色）
    public float changeInterval = 5f;    // 形状切换间隔（秒）
    public float responseTime = 4f;      // 玩家响应时间（需小于切换间隔）

    [Header("摄像头与小地图")]
    public Camera princessCamera;        // 看向公主的摄像头
    public RawImage minimapDisplay;      // 右上角小地图显示区域
    public float cameraFollowSpeed = 5f; // 摄像头跟随平滑度

    [Header("玩家引用")]
    public LinkShapeShrink playerScript; // 玩家的缩放脚本
    private Transform _playerTransform;
    private int _currentActiveShape = -1; // 0=圆 1=三角 2=方块 -1=无
    private bool _isPlayerInContact = false;

    void Start()
    {
        _playerTransform = playerScript.GetComponent<Transform>();
        // 初始化所有形状为灰色
        SetAllShapesInactive();
        // 开始随机闪烁协程
        StartCoroutine(RandomShapeBlink());
        // 初始化摄像头
        if (princessCamera != null)
            princessCamera.enabled = true;
        if (minimapDisplay != null)
            minimapDisplay.texture = princessCamera.targetTexture;
    }

    void Update()
    {
        // 摄像头跟随公主
        if (princessCamera != null && !_isPlayerInContact)
        {
            Vector3 targetPos = new Vector3(transform.position.x, transform.position.y + 2f, princessCamera.transform.position.z);
            princessCamera.transform.position = Vector3.Lerp(princessCamera.transform.position, targetPos, cameraFollowSpeed * Time.deltaTime);
        }

        // 检测玩家是否接触公主（用碰撞/触发，这里示例用距离检测）
        float distToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        if (distToPlayer < 1.5f)
        {
            _isPlayerInContact = true;
            HideBubbleAndCamera();
        }
        else
        {
            _isPlayerInContact = false;
            ShowBubbleAndCamera();
        }
    }

    // 随机激活一个形状
    IEnumerator RandomShapeBlink()
    {
        while (!_isPlayerInContact)
        {
            // 随机选择一个形状
            int newShape = Random.Range(0, 3);
            // 先重置所有形状
            SetAllShapesInactive();
            // 激活新形状
            ActivateShape(newShape);
            // 记录当前激活形状
            _currentActiveShape = newShape;
            // 等待响应时间后检查玩家是否匹配
            yield return new WaitForSeconds(responseTime);
            // 检查玩家形状是否匹配
            if (!IsPlayerShapeMatch() && !_isPlayerInContact)
            {
                // 不匹配 → 玩家缩小到当前大小的0.9倍
                playerScript.ForceShrink(0.9f);
                Debug.Log("玩家未及时变形，触发缩小惩罚！");
            }
            // 等待剩余时间到下一轮
            yield return new WaitForSeconds(changeInterval - responseTime);
        }
    }

    // 设置所有形状为未激活（灰色）
    void SetAllShapesInactive()
    {
        circleImg.color = inactiveColor;
        triangleImg.color = inactiveColor;
        squareImg.color = inactiveColor;
    }

    // 激活指定形状（变绿色）
    void ActivateShape(int shapeIndex)
    {
        switch (shapeIndex)
        {
            case 0: circleImg.color = activeColor; break;
            case 1: triangleImg.color = activeColor; break;
            case 2: squareImg.color = activeColor; break;
        }
    }

    // 检查玩家当前形状是否匹配激活的形状
    bool IsPlayerShapeMatch()
    {
        // 这里需要你在 LinkShapeShrink 脚本中添加当前形状标记
        // 示例：假设 playerScript.currentShape 为 0/1/2
        return playerScript.currentShape == _currentActiveShape;
    }

    // 隐藏气泡和摄像头
    void HideBubbleAndCamera()
    {
        if (bubbleUI != null) bubbleUI.SetActive(false);
        if (princessCamera != null) princessCamera.enabled = false;
        if (minimapDisplay != null) minimapDisplay.gameObject.SetActive(false);
    }

    // 显示气泡和摄像头
    void ShowBubbleAndCamera()
    {
        if (bubbleUI != null) bubbleUI.SetActive(true);
        if (princessCamera != null) princessCamera.enabled = true;
        if (minimapDisplay != null) minimapDisplay.gameObject.SetActive(true);
    }

    // 供外部获取当前激活的形状索引
    public int GetCurrentActiveShape()
    {
        return _currentActiveShape;
    }
}
