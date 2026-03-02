# Unity 开发规范指南

## 目录
- [命名规范](#命名规范)
- [代码结构规范](#代码结构规范)
- [Unity组件最佳实践](#unity组件最佳实践)
- [性能优化规范](#性能优化规范)
- [项目组织规范](#项目组织规范)
- [注释与文档规范](#注释与文档规范)
- [序列化与Inspector规范](#序列化与inspector规范)
- [协程与异步规范](#协程与异步规范)
- [资源管理规范](#资源管理规范)
- [测试与调试规范](#测试与调试规范)

---

## 命名规范

### 类和结构体
- 使用 **PascalCase**（帕斯卡命名法）
- 使用描述性名称，清晰表达类的职责
```csharp
public class PlayerController { }
public class AudioManager { }
public struct HandLandmarkData { }
```

### 接口
- 使用 **I** 前缀 + PascalCase
```csharp
public interface IInteractable { }
public interface IDamageable { }
```

### 方法
- 使用 **PascalCase**
- 使用动词或动词短语
```csharp
public void MovePlayer() { }
public bool TryGetComponent() { }
private void InitializeAudio() { }
```

### 变量和字段

#### 私有字段
- 使用 **camelCase** 或 **_camelCase**（下划线前缀）
```csharp
private float moveSpeed;
private Transform _targetTransform;
private AudioSource _audioSource;
```

#### 公共字段和属性
- 使用 **PascalCase**
```csharp
public int Health { get; set; }
public float MoveSpeed = 5f;
```

#### 常量
- 使用 **UPPER_SNAKE_CASE**
```csharp
private const float MAX_SPEED = 10f;
public const int DEFAULT_HEALTH = 100;
```

### Unity事件方法
- 保持Unity内置方法名称不变
```csharp
void Awake() { }
void Start() { }
void Update() { }
void OnEnable() { }
void OnDisable() { }
void OnDestroy() { }
```

---

## 代码结构规范

### 类成员组织顺序
```csharp
public class ExampleClass : MonoBehaviour
{
    // 1. 序列化字段（Inspector可见）
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform targetPoint;
    
    // 2. 常量
    private const float MAX_DISTANCE = 100f;
    
    // 3. 静态字段
    private static int instanceCount = 0;
    
    // 4. 公共属性
    public bool IsActive { get; private set; }
    
    // 5. 私有字段
    private Rigidbody _rigidbody;
    private AudioSource _audioSource;
    
    // 6. Unity生命周期方法（按调用顺序）
    void Awake() { }
    void OnEnable() { }
    void Start() { }
    void Update() { }
    void FixedUpdate() { }
    void LateUpdate() { }
    void OnDisable() { }
    void OnDestroy() { }
    
    // 7. 公共方法
    public void Initialize() { }
    public void Execute() { }
    
    // 8. 私有方法
    private void ProcessMovement() { }
    private void UpdateAudio() { }
    
    // 9. 协程
    private IEnumerator DelayedAction() { }
    
    // 10. 事件处理方法
    private void OnCollisionEnter(Collision collision) { }
    private void OnTriggerEnter(Collider other) { }
}
```

### 文件组织
- 每个文件只包含一个公共类
- 文件名必须与类名完全匹配
- 嵌套类和辅助结构可以在同一文件中

---

## Unity组件最佳实践

### 组件缓存
在 `Awake()` 中缓存组件引用，避免重复调用 `GetComponent()`
```csharp
private Rigidbody _rigidbody;
private AudioSource _audioSource;

void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();
    _audioSource = GetComponent<AudioSource>();
    
    if (_rigidbody == null)
    {
        Debug.LogError($"Rigidbody component missing on {gameObject.name}");
    }
}
```

### 空引用检查
```csharp
void Start()
{
    if (targetPoint == null)
    {
        Debug.LogWarning($"{nameof(targetPoint)} is not assigned on {gameObject.name}");
        return;
    }
}
```

### Transform访问优化
缓存Transform引用
```csharp
private Transform _transform;

void Awake()
{
    _transform = transform; // 缓存transform引用
}

void Update()
{
    _transform.position += Vector3.forward * Time.deltaTime;
}
```

### 使用 RequireComponent
确保必需的组件存在
```csharp
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    // ...
}
```

---

## 性能优化规范

### Update/FixedUpdate优化

#### 避免在Update中频繁查找对象
```csharp
// ❌ 错误示范
void Update()
{
    GameObject.Find("Player").transform.position = Vector3.zero;
}

// ✅ 正确做法
private Transform _playerTransform;

void Awake()
{
    _playerTransform = GameObject.Find("Player").transform;
}

void Update()
{
    if (_playerTransform != null)
    {
        _playerTransform.position = Vector3.zero;
    }
}
```

#### 使用Tag比较而非字符串
```csharp
// ❌ 错误示范
if (other.gameObject.tag == "Player") { }

// ✅ 正确做法
if (other.CompareTag("Player")) { }
```

### 对象池
频繁创建和销毁的对象使用对象池
```csharp
public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public GameObject GetObject()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(prefab);
    }
    
    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

### 避免内存分配
```csharp
// ❌ 避免在Update中创建新对象
void Update()
{
    Vector3 newPosition = new Vector3(x, y, z); // 每帧分配
}

// ✅ 使用字段或静态变量
private Vector3 _cachedPosition;

void Update()
{
    _cachedPosition.Set(x, y, z); // 重用已有对象
}
```

### Camera.main优化
```csharp
// Camera.main每次调用都会查找，应该缓存
private Camera _mainCamera;

void Awake()
{
    _mainCamera = Camera.main;
}
```

---

## 项目组织规范

### 文件夹结构
```
Assets/
├── Scenes/           # 场景文件
├── Scripts/          # 脚本文件
│   ├── Core/         # 核心系统脚本
│   ├── Managers/     # 管理器脚本
│   ├── UI/           # UI相关脚本
│   ├── Player/       # 玩家相关脚本
│   ├── Audio/        # 音频相关脚本
│   └── Utilities/    # 工具类脚本
├── Prefabs/          # 预制体
├── Materials/        # 材质
├── Textures/         # 纹理
├── Models/           # 3D模型
├── Audio/            # 音频文件
│   ├── Music/
│   ├── SFX/
│   └── Voice/
├── Animations/       # 动画文件
├── Fonts/            # 字体
├── Resources/        # 运行时加载资源（谨慎使用）
└── Plugins/          # 第三方插件
```

### 场景组织
场景中的GameObject应该有清晰的层级结构
```
Scene
├── --- Management ---
│   ├── GameManager
│   ├── AudioManager
│   └── UIManager
├── --- Environment ---
│   ├── Lighting
│   ├── Ground
│   └── Props
├── --- Dynamic Objects ---
│   ├── Player
│   ├── Enemies
│   └── Projectiles
└── --- UI ---
    ├── Canvas
    └── EventSystem
```

---

## 注释与文档规范

### 类注释
```csharp
/// <summary>
/// 管理玩家的移动、输入和动画
/// </summary>
public class PlayerController : MonoBehaviour
{
}
```

### 方法注释
```csharp
/// <summary>
/// 初始化玩家的组件和初始状态
/// </summary>
/// <param name="startPosition">玩家的起始位置</param>
/// <returns>初始化是否成功</returns>
public bool Initialize(Vector3 startPosition)
{
    // 实现
}
```

### 复杂逻辑注释
```csharp
// 计算玩家的移动方向，考虑摄像机的朝向
// 这样可以实现相对于摄像机的移动控制
Vector3 forward = _mainCamera.transform.forward;
forward.y = 0f;
forward.Normalize();
```

### TODO注释
```csharp
// TODO: 添加跳跃功能
// FIXME: 修复碰撞检测的边界问题
// OPTIMIZE: 优化寻路算法性能
// NOTE: 这个方法会在下一个版本中被弃用
```

---

## 序列化与Inspector规范

### 使用Header和Tooltip
```csharp
[Header("Movement Settings")]
[Tooltip("玩家的最大移动速度")]
[SerializeField] private float maxSpeed = 10f;

[Tooltip("加速度")]
[SerializeField] private float acceleration = 2f;

[Header("Audio Settings")]
[SerializeField] private AudioClip jumpSound;
```

### 使用Range限制数值
```csharp
[Range(0f, 1f)]
[SerializeField] private float volume = 0.8f;

[Range(1, 100)]
[SerializeField] private int health = 100;
```

### 序列化字段规范
```csharp
// ✅ 推荐：使用 [SerializeField] 使私有字段可见
[SerializeField] private float moveSpeed = 5f;

// ❌ 避免：不要为了Inspector可见而使用public字段
public float moveSpeed = 5f; // 破坏封装性

// ✅ 如果需要公共访问，使用属性
[SerializeField] private float moveSpeed = 5f;
public float MoveSpeed => moveSpeed;
```

### 使用 HideInInspector
```csharp
[HideInInspector]
public bool isInitialized; // 公共但不在Inspector显示
```

---

## 协程与异步规范

### 协程最佳实践
```csharp
// 缓存WaitForSeconds避免重复创建
private WaitForSeconds _waitOneSecond;

void Awake()
{
    _waitOneSecond = new WaitForSeconds(1f);
}

private IEnumerator DelayedAction()
{
    yield return _waitOneSecond;
    // 执行延迟操作
}

// 存储协程引用以便停止
private Coroutine _activeCoroutine;

public void StartDelayedAction()
{
    if (_activeCoroutine != null)
    {
        StopCoroutine(_activeCoroutine);
    }
    _activeCoroutine = StartCoroutine(DelayedAction());
}
```

### 使用 async/await（Unity 2021+）
```csharp
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AsyncExample : MonoBehaviour
{
    private CancellationTokenSource _cts;
    
    void OnEnable()
    {
        _cts = new CancellationTokenSource();
        LoadDataAsync(_cts.Token);
    }
    
    void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
    
    private async void LoadDataAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(1000, token);
            // 加载数据
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("Operation cancelled");
        }
    }
}
```

---

## 资源管理规范

### 使用 Resources.Load（谨慎）
```csharp
// Resources文件夹会增加包体积，优先使用AssetBundle或Addressables
private AudioClip LoadAudioClip(string clipName)
{
    AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
    if (clip == null)
    {
        Debug.LogError($"Failed to load audio clip: {clipName}");
    }
    return clip;
}
```

### 资源释放
```csharp
void OnDestroy()
{
    // 释放动态加载的资源
    if (_loadedTexture != null)
    {
        Resources.UnloadAsset(_loadedTexture);
        _loadedTexture = null;
    }
}
```

### Addressables（推荐）
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : MonoBehaviour
{
    [SerializeField] private AssetReference audioClipReference;
    private AsyncOperationHandle<AudioClip> _loadHandle;
    
    async void Start()
    {
        _loadHandle = audioClipReference.LoadAssetAsync<AudioClip>();
        await _loadHandle.Task;
        
        if (_loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip clip = _loadHandle.Result;
            // 使用资源
        }
    }
    
    void OnDestroy()
    {
        if (_loadHandle.IsValid())
        {
            Addressables.Release(_loadHandle);
        }
    }
}
```

---

## 测试与调试规范

### 条件编译
```csharp
#if UNITY_EDITOR
    [SerializeField] private bool debugMode = true;
#endif

void Update()
{
#if UNITY_EDITOR
    if (debugMode)
    {
        Debug.DrawRay(transform.position, transform.forward * 10f, Color.red);
    }
#endif
}
```

### Debug日志规范
```csharp
// 使用不同级别的日志
Debug.Log("普通信息");
Debug.LogWarning("警告信息");
Debug.LogError("错误信息");

// 包含上下文信息
Debug.Log($"Player health: {health}", this);

// 使用条件日志
[System.Diagnostics.Conditional("UNITY_EDITOR")]
private void DebugLog(string message)
{
    Debug.Log(message);
}
```

### Gizmos可视化
```csharp
void OnDrawGizmos()
{
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
}

void OnDrawGizmosSelected()
{
    // 仅在选中时绘制
    Gizmos.color = Color.red;
    Gizmos.DrawLine(transform.position, targetPosition);
}
```

### 单元测试
```csharp
using NUnit.Framework;
using UnityEngine;

public class PlayerTests
{
    [Test]
    public void Player_InitialHealth_IsCorrect()
    {
        GameObject playerObj = new GameObject();
        Player player = playerObj.AddComponent<Player>();
        
        Assert.AreEqual(100, player.Health);
        
        Object.DestroyImmediate(playerObj);
    }
}
```

---

## 其他重要规范

### 使用事件和委托
```csharp
using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 使用UnityEvent或C#事件
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDied;
    
    private int _health = 100;
    
    public void TakeDamage(int damage)
    {
        _health -= damage;
        OnHealthChanged?.Invoke(_health);
        
        if (_health <= 0)
        {
            OnPlayerDied?.Invoke();
        }
    }
}
```

### 单例模式（谨慎使用）
```csharp
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                
                if (_instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    _instance = obj.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

### ScriptableObject 用于数据
```csharp
[CreateAssetMenu(fileName = "New Weapon", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Properties")]
    public string weaponName;
    public int damage;
    public float fireRate;
    public AudioClip fireSound;
    public GameObject projectilePrefab;
}
```

---

## 代码审查清单

在提交代码前，请确保：

- [ ] 所有公共API都有XML文档注释
- [ ] 没有使用magic numbers（使用常量或配置）
- [ ] 所有的GetComponent调用都已缓存
- [ ] 没有在Update/FixedUpdate中进行不必要的计算
- [ ] 所有协程都有正确的停止逻辑
- [ ] 序列化字段都有合适的Header和Tooltip
- [ ] 没有内存泄漏（事件订阅已取消，资源已释放）
- [ ] 代码遵循项目的命名规范
- [ ] 没有警告或错误
- [ ] 代码已经过测试

---

## 参考资源

- [Unity Manual](https://docs.unity3d.com/Manual/index.html)
- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- [Unity Best Practices](https://unity.com/how-to/programming-unity)

---

**版本**: 1.0  
**最后更新**: 2026-02-01











