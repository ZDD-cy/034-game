using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LinkController : MonoBehaviour
{
    private bool _hasPermanentlyShrunk = false; // 永久缩放锁定标记（新增）
    private bool _hasPermanentlyChanged = false; // 永久变形标记（之前要求的）
    private float _shapeChangeBaseSize; // 记录「变形那一刻」的玩家大小（作为缩放宽基）
    [Header("链接设置")]
    public float ropeDefaultLength = 2f;
    public float ropeForce = 15f;

    [Header("大小比例参数")]
    public float playerSize = 1f; // 你的大小参数

    private Rigidbody2D _rb;
    private GameObject _linkedTarget;
    private bool _isLinking;
    private LineRenderer _line;
    [Header("变形设置")]
    public Sprite defaultPlayerSprite; // 玩家默认的精灵
    public float shapeChangeDelay = 0.5f; // 开始变形的延迟时间（在超过阈值后多久开始变）
    private Vector3 _targetScale;
   
    private SpriteRenderer _playerSpriteRenderer; // 玩家的SpriteRenderer组件
    private Sprite _targetShapeSprite; // 目标形状的精灵
    private bool _isShapeChanged = false; // 是否已变形

    [Header("链接超时设置")]
    public float linkDurationThreshold = 1f; // 链接持续时间阈值，单位秒
    public float sizeReductionPerSecond = 0.1f; // 每秒减少的大小参数
    public float minPlayerSize = 0.9f; // 玩家最小大小，防止缩到0
    public float shrinkAnimationSpeed = 2f; // 缩小动画的速度

    // 用于判断链接对象是否为地面/墙的标签
    public string groundTag = "Ground";
    public string wallTag = "Wall";

    // 内部状态
    private float _currentLinkDuration = 0f; // 当前链接持续时间
    private float _targetPlayerSize; // 用于平滑过渡的目标大小
  
                                        
  
  


    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.positionCount = 2;
        _line.startWidth = 0.1f;
        _line.enabled = false;

        _targetPlayerSize = playerSize;
        _targetScale = transform.localScale;

        // 初始化变形相关组件
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (_playerSpriteRenderer == null)
        {
            Debug.LogError("玩家对象缺少SpriteRenderer组件！");
        }
        else
        {
            defaultPlayerSprite = _playerSpriteRenderer.sprite;
        }
        _rb = GetComponent<Rigidbody2D>();
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.positionCount = 2;
        _line.startWidth = 0.1f;
        _line.enabled = false;
        // 新增：初始化目标大小为「玩家当前缩放的X轴」（2D物体缩放通常均匀）
        _targetPlayerSize = transform.localScale.x;
        // 初始化基准大小为当前大小
        _shapeChangeBaseSize = transform.localScale.x;
    }



    void Update()

    {

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("鼠标左键被按下");
            TryLinkTarget();
        }

        if (Input.GetMouseButton(0) && _linkedTarget != null)
        {
            _isLinking = true;
            UpdateRopeVisual();
        }
        else
        {
            BreakLink();

        }

        // 点击选中
        if (Input.GetMouseButtonDown(0))
        {
            TryLinkTarget();
        }

        // 长按维持
        if (Input.GetMouseButton(0) && _linkedTarget != null)
        {
            _isLinking = true;
            UpdateRopeVisual();
        }
        else
        {
            BreakLink();
        }
    }

    void FixedUpdate()
    {
        if (_isLinking && _linkedTarget != null)
        {
            ApplyLinkForce();
            UpdateLinkDurationAndShrink();
        }
        else
        {
            // 当链接断开时，重置计时器和大小
            _currentLinkDuration = 0f;
            _targetScale = Vector3.one; // 假设初始缩放为1
        }

        // 平滑过渡到目标大小和缩放
        playerSize = Mathf.Lerp(playerSize, _targetPlayerSize, shrinkAnimationSpeed * Time.fixedDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, shrinkAnimationSpeed * Time.fixedDeltaTime);



    } 

    // 射线选中目标
    void TryLinkTarget()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D ray = Physics2D.Raycast(mousePos, Vector2.zero);

        if (ray && ray.collider.CompareTag("Linkable"))
        {
            _linkedTarget = ray.collider.gameObject;
            _line.enabled = true;
        }
    }

    // 弹力计算
    void ApplyLinkForce()
    {
        Rigidbody2D targetRb = _linkedTarget.GetComponent<Rigidbody2D>();
        LinkableTarget linkable = _linkedTarget.GetComponent<LinkableTarget>();
        if (targetRb == null || linkable == null) return;

        Vector2 dir = _linkedTarget.transform.position - transform.position;
        float dist = dir.magnitude;
        Vector2 dirNorm = dir.normalized;

        float offset = dist - ropeDefaultLength;
        if (offset < 0) offset = 0;

        Vector2 force = dirNorm * offset * ropeForce;

        // 按大小比例分配受力（你要的参数）
        float totalSize = playerSize + linkable.targetSize;
        Vector2 playerForce = force * (linkable.targetSize / totalSize);
        Vector2 targetForce = -force * (playerSize / totalSize);

        _rb.AddForce(playerForce);
        targetRb.AddForce(targetForce);
    }

    // 绳子渲染
    void UpdateRopeVisual()
    {
        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, _linkedTarget.transform.position);
        if (_linkedTarget != null)
        {
            Debug.Log("Updating rope from " + transform.position + " to " + _linkedTarget.transform.position);
            _line.SetPosition(0, transform.position);
            _line.SetPosition(1, _linkedTarget.transform.position);
        }
    }
    // 这个函数负责更新链接时间，并在超过阈值后触发缩小和变形
    void UpdateLinkDurationAndShrink()
    {
        // 安全校验：目标为空直接退出（避免空引用）
        if (_linkedTarget == null) return;

        // 如果链接对象是地面或墙，则不进行缩小和变形
        if (_linkedTarget.CompareTag(groundTag) || _linkedTarget.CompareTag(wallTag))
        {
            _currentLinkDuration = 0f;
            return;
        }

        // 增加链接时间
        _currentLinkDuration += Time.fixedDeltaTime;

        // 打印当前链接时间（仅每0.1秒打印一次，避免控制台刷屏）
        if (Time.frameCount % 6 == 0)
        {
            Debug.Log($"当前链接时间: {_currentLinkDuration:F2} 秒");
        }

        // 5. 超过阈值后执行：先缩小，后变形，变形后锁定缩放
        if (_currentLinkDuration > linkDurationThreshold && !_hasPermanentlyShrunk)
        {
            // ===== 核心缩放逻辑：缩放到「变形基准大小的90%」=====
            // 变形触发前：持续缩小，最小值 = _shapeChangeBaseSize * 0.9
            float minSizeForThisShape = _shapeChangeBaseSize * 0.9f;
            // 持续缩小目标大小
            _targetPlayerSize -= sizeReductionPerSecond * Time.fixedDeltaTime;
            // 锁定最小值为当前基准的90%
            _targetPlayerSize = Mathf.Max(_targetPlayerSize, minSizeForThisShape);

            // 修复缩放抖动：直接基于当前基准设置目标缩放（避免比例累积错误）
            _targetScale = Vector3.one * _targetPlayerSize;

            // ===== 变形触发逻辑：达到延迟时间后执行 =====
            if (_currentLinkDuration > linkDurationThreshold + shapeChangeDelay && !_isShapeChanged && !_hasPermanentlyChanged)
            {
                // 变形时：更新「下一次缩放的基准大小」为「当前变形后的大小」
                _shapeChangeBaseSize = _targetPlayerSize;
                // 执行变形
                ChangeToLinkedShape();
                // 变形后锁定缩放，本次链接不再缩小
                _hasPermanentlyShrunk = true;
                Debug.Log($"变形成功！新的缩放基准为: {_shapeChangeBaseSize:F2}，最小缩放为: {_shapeChangeBaseSize * 0.9f:F2}");
            }
        

        // 2. 处理形状变化：满足时间条件且未变形时执行
        if (_currentLinkDuration > linkDurationThreshold + shapeChangeDelay && !_isShapeChanged && !_hasPermanentlyChanged)
            {
                ChangeToLinkedShape();
            }
        }
    }


    // 变成所链接的形状
    void ChangeToLinkedShape()
        {

            if (_linkedTarget == null)
            {
                Debug.LogError("链接目标为空，无法变形！");
                return;
            }

            SpriteRenderer targetSpriteRenderer = _linkedTarget.GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer == null)
            {
                Debug.LogError($"目标对象 {_linkedTarget.name} 缺少 SpriteRenderer 组件！");
                return;
            }

            if (targetSpriteRenderer.sprite == null)
            {
                Debug.LogError($"目标对象 {_linkedTarget.name} 的 SpriteRenderer 没有赋值 Sprite！");
                return;
            }
            Debug.Log($"时间: {_currentLinkDuration:F2}, 阈值+延迟: {linkDurationThreshold + shapeChangeDelay:F2}, 是否变形过: {_isShapeChanged}");

            _targetShapeSprite = targetSpriteRenderer.sprite;
            _playerSpriteRenderer.sprite = _targetShapeSprite;
            _isShapeChanged = true;
            Debug.Log($"玩家成功变成了 {_linkedTarget.name} 的形状！");


            if (_linkedTarget == null) return;


        }


        // 恢复到原始形状
        void RevertToOriginalShape()
        {
            if (_playerSpriteRenderer != null)
            {
                _playerSpriteRenderer.sprite = defaultPlayerSprite;
                _isShapeChanged = false;
                Debug.Log("玩家恢复了原始形状");
            }
        }

        // 断开
        void BreakLink()
        {
            _isLinking = false;
            _linkedTarget = null;
            _line.enabled = false;
        // 断开链接时，恢复原始形状和大小
        // 重置：单次链接的计时和缩放锁（关键！让下次链接重新计算）
        _currentLinkDuration = 0f;
        _hasPermanentlyShrunk = false;

        // 保留：永久变形和当前大小（不重置 _targetPlayerSize 和 _shapeChangeBaseSize）
        Debug.Log("链接断开，保留当前大小和形状，下次链接将以当前大小为基准缩小到90%");
    
}
    }

