using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLinkShrinkShape : MonoBehaviour
{
    // 永久状态
    private bool _hasPermanentlyShrunk = false;   // 本次链接是否已缩放过
    private bool _hasPermanentlyChanged = false; // 是否永久变形（你要的保留变形）
    private float _shapeChangeBaseSize;          // 变形基准大小

    [Header("绳索设置")]
    public float ropeDefaultLength = 2f;
    public float ropeForce = 15f;

    [Header("大小参数")]
    public float playerSize = 1f;
    private float _targetPlayerSize;

    [Header("变形设置")]
    public Sprite defaultPlayerSprite;
    public float shapeChangeDelay = 0.5f;
    private Vector3 _targetScale;
    private SpriteRenderer _playerSpriteRenderer;
    private bool _isShapeChanged = false; // 单次链接是否变形

    [Header("链接缩小阈值")]
    public float linkDurationThreshold = 1f;
    public float sizeReductionPerSecond = 0.1f;
    public float minPlayerSize = 0.9f;
    public float shrinkAnimationSpeed = 2f;

    [Header("标签")]
    public string groundTag = "Ground";
    public string wallTag = "Wall";

    private Rigidbody2D _rb;
    private GameObject _linkedTarget;
    private bool _isLinking;
    private LineRenderer _line;
    private float _currentLinkDuration = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (_playerSpriteRenderer != null)
            defaultPlayerSprite = _playerSpriteRenderer.sprite;

        _line = GetComponent<LineRenderer>();
        if (_line == null)
            _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.1f;
        _line.enabled = false;

        _targetPlayerSize = transform.localScale.x;
        _shapeChangeBaseSize = transform.localScale.x;
        _targetScale = transform.localScale;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
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
            _currentLinkDuration = 0f;
        }

        // 平滑缩放
        playerSize = Mathf.Lerp(playerSize, _targetPlayerSize, shrinkAnimationSpeed * Time.fixedDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, shrinkAnimationSpeed * Time.fixedDeltaTime);
    }

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

    void ApplyLinkForce()
    {
        Rigidbody2D targetRb = _linkedTarget.GetComponent<Rigidbody2D>();
        LinkableTarget linkable = _linkedTarget.GetComponent<LinkableTarget>();
        if (targetRb == null || linkable == null) return;

        Vector2 dir = _linkedTarget.transform.position - transform.position;
        float dist = dir.magnitude;
        Vector2 dirNorm = dir.normalized;
        float offset = Mathf.Max(dist - ropeDefaultLength, 0);
        Vector2 force = dirNorm * offset * ropeForce;

        float totalSize = playerSize + linkable.targetSize;
        Vector2 playerForce = force * (linkable.targetSize / totalSize);
        Vector2 targetForce = -force * (playerSize / totalSize);

        _rb.AddForce(playerForce);
        targetRb.AddForce(targetForce);
    }

    void UpdateRopeVisual()
    {
        if (_linkedTarget != null)
        {
            _line.SetPosition(0, transform.position);
            _line.SetPosition(1, _linkedTarget.transform.position);
        }
    }

    void UpdateLinkDurationAndShrink()
    {
        if (_linkedTarget == null) return;

        // 墙/地面不缩小
        if (_linkedTarget.CompareTag(groundTag) || _linkedTarget.CompareTag(wallTag))
        {
            _currentLinkDuration = 0f;
            return;
        }

        _currentLinkDuration += Time.fixedDeltaTime;

        // 超过阈值开始缩小
        if (_currentLinkDuration > linkDurationThreshold && !_hasPermanentlyShrunk)
        {
            float minSizeForThisShape = _shapeChangeBaseSize * 0.9f;
            _targetPlayerSize -= sizeReductionPerSecond * Time.fixedDeltaTime;
            _targetPlayerSize = Mathf.Max(_targetPlayerSize, minSizeForThisShape);
            _targetScale = Vector3.one * _targetPlayerSize;

            // 变形条件（只执行一次）
            if (_currentLinkDuration > linkDurationThreshold + shapeChangeDelay && !_isShapeChanged)
            {
                _shapeChangeBaseSize = _targetPlayerSize;
                ChangeToLinkedShape();
             
            }
        }
    }

    void ChangeToLinkedShape()
    {
        if (_linkedTarget == null) return;

        SpriteRenderer targetSr = _linkedTarget.GetComponent<SpriteRenderer>();
        if (targetSr == null || targetSr.sprite == null) return;

        _playerSpriteRenderer.sprite = targetSr.sprite;
        _isShapeChanged = true;       // 标记本次已变形
        _hasPermanentlyChanged = true;// 永久变形
        Debug.Log("变形成功！");
    }

    void BreakLink()
    {
        _isLinking = false;
        _linkedTarget = null;
        _line.enabled = false;

        // 关键修复：每次断开都重置单次状态
        _currentLinkDuration = 0f;
        _hasPermanentlyShrunk = false;
        _isShapeChanged = false; // 允许下次再次变形

        Debug.Log("链接断开，下次可重新变形");
    }
}

