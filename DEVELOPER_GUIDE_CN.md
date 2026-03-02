# 🛠️ 乐器交互系统 - 开发者指南

> 本指南教您如何自主添加和修改系统功能

---

## 📊 系统架构概览

```
系统架构：
┌─────────────────────────────────────────────────┐
│              InstrumentInputManager              │  输入层
│        (统一处理鼠标/触摸输入，射线检测)          │
└────────────────┬────────────────────────────────┘
                 │
       ┌─────────┴─────────┐
       │                   │
┌──────▼──────┐    ┌──────▼──────┐                 控制层
│GuqinController│  │PipaController│
│(古琴逻辑)     │  │(琵琶逻辑)     │
└──────┬──────┘    └──────┬──────┘
       │                   │
       └─────────┬─────────┘
                 │
         ┌───────▼───────┐                          琴弦层
         │InstrumentString│
         │  (单根琴弦)    │
         └───────┬───────┘
                 │
         ┌───────▼───────┐                          音频层
         │InstrumentAudio│
         │   Manager     │
         └───────────────┘
```

---

## 📁 脚本职责说明

### 🎯 核心脚本

| 脚本名称 | 主要职责 | 修改频率 |
|---------|---------|---------|
| `InstrumentString.cs` | 单根琴弦的行为（播放音频、视觉反馈、震动） | ⭐⭐⭐ 高 |
| `GuqinController.cs` | 古琴特定逻辑（7弦、13品、交互处理） | ⭐⭐ 中 |
| `PipaController.cs` | 琵琶特定逻辑（4弦、24品、扫弦） | ⭐⭐ 中 |
| `InstrumentInputManager.cs` | 输入处理、射线检测、手势识别 | ⭐⭐ 中 |
| `InstrumentAudioManager.cs` | 音频加载、生成、缓存 | ⭐ 低 |

### 🔧 工具脚本

| 脚本名称 | 主要职责 | 何时使用 |
|---------|---------|---------|
| `InstrumentSetupHelper.cs` | 编辑器辅助工具（快速创建琴弦） | 初始配置 |
| `InstrumentDebugger.cs` | 诊断和调试工具 | 故障排查 |
| `InstrumentTagSetup.cs` | 自动设置Unity标签 | 初始配置 |
| `InstrumentSceneSetup.cs` | 一键场景设置 | 初始配置 |

---

## 🎨 常见功能修改指南

### 1️⃣ 修改视觉效果

#### ⭐ 场景：让模型琴弦震动（而不是碰撞琴弦）

> **重要功能！** 如果你有单独的视觉模型琴弦和碰撞琴弦

**修改文件：** 在 Unity Inspector 中配置，无需改代码

**步骤：**

1. **准备你的琴弦结构**

```
场景结构示例：
├── Guqin (古琴模型)
│   ├── VisualString_1 (视觉模型琴弦) ← 你看到的漂亮琴弦
│   ├── VisualString_2
│   ├── ...
│   ├── CollisionString_1 (碰撞琴弦，添加 InstrumentString 组件) ← 用于检测点击
│   ├── CollisionString_2
│   └── ...
```

2. **配置 InstrumentString 组件**

选中碰撞琴弦（如 `CollisionString_1`），在 Inspector 中：

```
InstrumentString 组件：
┌────────────────────────────────────┐
│ 🎨 模型琴弦设置                     │
├────────────────────────────────────┤
│ Visual String Object:              │
│   ▶ VisualString_1 (Transform)     │ ← 拖入你的模型琴弦！
│                                    │
│ ☑ Also Vibrate Collider           │ ← 是否同时震动碰撞琴弦
└────────────────────────────────────┘
```

3. **参数说明**

- **Visual String Object**: 
  - 拖入你的**模型琴弦**（视觉模型）
  - 震动效果和颜色变化会应用到这个物体上
  - 如果留空，则震动脚本所在的物体（碰撞琴弦）

- **Also Vibrate Collider**: 
  - ✅ 勾选：模型琴弦和碰撞琴弦**都会震动**
  - ❌ 不勾选：**只有模型琴弦**震动（推荐）

4. **效果预览**

```
配置前：
- 点击琴弦 → 碰撞琴弦震动 ❌（你看不到，因为它被隐藏或重合）

配置后：
- 点击琴弦 → 模型琴弦震动 ✅（你能看到漂亮的震动效果！）
```

**完整示例（代码）：**

如果你需要在代码中动态设置：

```csharp
// 在 GuqinController 中自动关联模型琴弦
void Start()
{
    for (int i = 0; i < strings.Length; i++)
    {
        if (strings[i] != null)
        {
            // 假设模型琴弦命名为 "VisualString_X"
            Transform visualString = transform.Find($"VisualString_{i + 1}");
            
            if (visualString != null)
            {
                strings[i].visualStringObject = visualString;
                strings[i].alsoVibrateCollider = false;
                Debug.Log($"✅ 弦 {i} 关联到模型琴弦: {visualString.name}");
            }
        }
    }
}
```

**调试日志：**

正确配置后，点击琴弦会看到：

```
✅ 弦 0 使用模型琴弦进行震动：VisualString_1
🎵 开始震动：VisualString_1
💫 显示视觉反馈：弦 0
✅ 弦颜色已改变为 RGBA(1.0, 1.0, 0.0, 1.0) (物体: VisualString_1)
✅ 震动完成：VisualString_1
```

---

#### 场景：改变琴弦被点击时的颜色

**修改文件：** `InstrumentString.cs`

**位置：** `ShowStringVibration()` 方法

```csharp
void ShowStringVibration()
{
    vibrationTime = vibrationDuration;
    
    if (stringRenderer != null && stringRenderer.material != null)
    {
        // 🔧 修改这里：改变触摸颜色
        stringRenderer.material.color = touchColor; // ← 这里
        
        // 💡 你可以添加更多效果：
        // stringRenderer.material.SetFloat("_Metallic", 0.8f);
        // stringRenderer.material.EnableKeyword("_EMISSION");
    }
    
    StartCoroutine(VibrateEffect());
}
```

**如何测试：**
1. 在 Inspector 中修改 `InstrumentString` 的 `Touch Color`
2. 或在代码中直接设置 `touchColor = new Color(0, 1, 0, 1);` (绿色)

---

#### 场景：修改琴弦震动效果

**修改文件：** `InstrumentString.cs`

**位置：** `VibrateEffect()` 协程

```csharp
System.Collections.IEnumerator VibrateEffect()
{
    Vector3 originalScale = transform.localScale;
    float elapsed = 0f;
    
    while (elapsed < vibrationDuration)
    {
        float progress = elapsed / vibrationDuration;
        
        // 🔧 修改这里：改变震动模式
        // 当前：正弦波震动
        float scale = 1f + Mathf.Sin(progress * Mathf.PI * 10) * 0.1f;
        
        // 💡 示例1：更强烈的震动
        // float scale = 1f + Mathf.Sin(progress * Mathf.PI * 20) * 0.2f;
        
        // 💡 示例2：衰减震动
        // float decay = 1f - progress;
        // float scale = 1f + Mathf.Sin(progress * Mathf.PI * 10) * 0.1f * decay;
        
        // 💡 示例3：只在X轴震动
        // transform.localScale = new Vector3(
        //     originalScale.x * scale,
        //     originalScale.y,
        //     originalScale.z
        // );
        
        transform.localScale = originalScale * scale;
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    transform.localScale = originalScale;
}
```

---

#### 场景：添加粒子特效

**修改文件：** `InstrumentString.cs`

**步骤：**

1. 在 `InstrumentString` 类添加字段：

```csharp
[Header("特效设置")]
[Tooltip("琴弦粒子特效")]
public ParticleSystem pluckParticles;
```

2. 修改 `ShowStringVibration()` 方法：

```csharp
void ShowStringVibration()
{
    vibrationTime = vibrationDuration;
    
    // 原有代码...
    if (stringRenderer != null && stringRenderer.material != null)
    {
        stringRenderer.material.color = touchColor;
    }
    
    // 🆕 添加粒子特效
    if (pluckParticles != null)
    {
        pluckParticles.Play();
    }
    
    StartCoroutine(VibrateEffect());
}
```

3. 在 Unity 中：
   - 为琴弦添加 Particle System 组件
   - 将其拖入 Inspector 的 `Pluck Particles` 字段

---

### 2️⃣ 修改音频效果

#### 场景：调整音量

**修改文件：** `InstrumentString.cs`

**位置：** `PluckString()` 方法

```csharp
public void PluckString()
{
    // ... 前面的代码 ...
    
    if (clipToPlay != null && audioSource != null)
    {
        audioSource.clip = clipToPlay;
        
        // 🔧 修改这里：调整音量
        audioSource.volume = 1.0f; // ← 改为 0.0 ~ 1.0 之间的值
        
        // 💡 示例：根据力度调整音量
        // audioSource.volume = pluckStrength; // 需要添加 pluckStrength 参数
        
        audioSource.Play();
        // ... 后续代码 ...
    }
}
```

---

#### 场景：添加音效淡出（Fade Out）

**修改文件：** `InstrumentString.cs`

**步骤：**

1. 添加字段：

```csharp
[Header("音频设置")]
[Tooltip("音频淡出时间")]
public float fadeOutDuration = 2.0f;
```

2. 修改 `PluckString()` 方法：

```csharp
public void PluckString()
{
    // ... 原有代码 ...
    
    if (clipToPlay != null && audioSource != null)
    {
        audioSource.clip = clipToPlay;
        audioSource.volume = 1.0f;
        audioSource.Play();
        
        // 🆕 添加淡出效果
        StartCoroutine(FadeOutAudio());
        
        // ... 后续代码 ...
    }
}
```

3. 添加新方法：

```csharp
System.Collections.IEnumerator FadeOutAudio()
{
    float startVolume = audioSource.volume;
    float elapsed = 0f;
    
    // 等待一会儿再开始淡出
    yield return new WaitForSeconds(0.5f);
    
    while (elapsed < fadeOutDuration)
    {
        elapsed += Time.deltaTime;
        audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
        yield return null;
    }
    
    audioSource.volume = 0f;
}
```

---

#### 场景：使用自定义音频文件

**修改文件：** 在 Unity Inspector 中配置，无需改代码

**步骤：**
1. 准备音频文件（WAV/MP3）
2. 放入 `Assets/Audio/Guqin/` 或 `Assets/Audio/Pipa/`
3. 选中琴弦 GameObject
4. 在 Inspector 的 `InstrumentString` 组件中：
   - 将音频拖入 `Open String Sound` (空弦音)
   - 将品位音频拖入 `Fret Sounds` 数组

---

### 3️⃣ 修改交互逻辑

#### 场景：添加双击检测

**修改文件：** `InstrumentInputManager.cs`

**步骤：**

1. 添加字段：

```csharp
[Header("手势设置")]
public float doubleClickTime = 0.3f; // 双击时间窗口

private float lastClickTime = 0f;
private GameObject lastClickedObject = null;
```

2. 修改 `OnInputDown()` 方法：

```csharp
void OnInputDown(Vector2 screenPos)
{
    // ... 原有射线检测代码 ...
    
    if (Physics.Raycast(ray, out hit, 100f, raycastLayerMask))
    {
        lastHit = hit;
        
        // 🆕 检测双击
        float timeSinceLastClick = Time.time - lastClickTime;
        bool isDoubleClick = timeSinceLastClick < doubleClickTime 
                             && lastClickedObject == hit.collider.gameObject;
        
        if (isDoubleClick)
        {
            // 触发双击事件
            OnDoubleClick(hit);
        }
        else
        {
            // 单击逻辑
            SendClickEvent(hit);
        }
        
        lastClickTime = Time.time;
        lastClickedObject = hit.collider.gameObject;
    }
}
```

3. 添加新方法：

```csharp
void OnDoubleClick(RaycastHit hit)
{
    if (showDebug)
        Debug.Log($"🔄 双击检测: {hit.collider.name}");
    
    // 💡 这里可以添加双击的特殊行为
    // 例如：播放和声、切换音色等
    
    if (hit.collider.CompareTag("InstrumentString"))
    {
        InstrumentString instrumentString = hit.collider.GetComponent<InstrumentString>();
        if (instrumentString != null)
        {
            // 例如：双击播放更强的音
            instrumentString.PluckString();
            instrumentString.PluckString(); // 快速连续播放
        }
    }
}
```

---

#### 场景：添加滑动琴弦（Glissando 滑音）

**修改文件：** `InstrumentInputManager.cs` 和 `InstrumentString.cs`

**步骤：**

1. 在 `InstrumentInputManager.cs` 修改 `OnInputDrag()` 方法：

```csharp
void OnInputDrag(Vector2 screenPos)
{
    if (!isDragging) return;
    
    Ray ray = GetRayFromScreenPosition(screenPos);
    RaycastHit hit;
    
    if (Physics.Raycast(ray, out hit, 100f, raycastLayerMask))
    {
        // 🆕 检测是否滑过琴弦
        if (hit.collider.CompareTag("InstrumentString"))
        {
            InstrumentString currentString = hit.collider.GetComponent<InstrumentString>();
            
            // 如果滑到了新的琴弦
            if (currentString != null && currentString != lastDraggedString)
            {
                currentString.PluckString();
                lastDraggedString = currentString;
                
                if (showDebug)
                    Debug.Log($"🎵 滑动到琴弦: {currentString.stringIndex}");
            }
        }
    }
}
```

2. 添加字段：

```csharp
private InstrumentString lastDraggedString = null;
```

3. 在 `OnInputUp()` 中重置：

```csharp
void OnInputUp(Vector2 screenPos)
{
    isDragging = false;
    longPressTriggered = false;
    lastDraggedString = null; // 🆕 重置
    
    // ... 原有代码 ...
}
```

---

### 4️⃣ 添加新乐器

#### 场景：添加二胡

**步骤：**

1. 创建新脚本 `ErhuController.cs`：

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ErhuController : MonoBehaviour
{
    [Header("琴弦配置")]
    [Tooltip("二胡的2根琴弦")]
    public InstrumentString[] strings = new InstrumentString[2];
    
    [Header("二胡特定设置")]
    public int numPositions = 12; // 把位数量
    public bool showDebug = true;
    
    void Start()
    {
        InitializeStrings();
        CreatePositionMarkers();
    }
    
    void InitializeStrings()
    {
        // 二胡定音：外弦 D4, 内弦 A4
        float[] basePitches = new float[] { 293.66f, 440.0f }; // D4, A4
        
        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] != null)
            {
                strings[i].stringIndex = i;
                strings[i].basePitch = basePitches[i];
                
                // 可以在这里生成把位音频
                // strings[i].fretSounds = GeneratePositionSounds(basePitches[i]);
            }
        }
        
        if (showDebug)
            Debug.Log($"✅ 二胡初始化完成：{strings.Length} 根弦");
    }
    
    void CreatePositionMarkers()
    {
        // 创建把位标记（类似 GuqinController 的 CreateFretColliders）
        // ... 实现逻辑 ...
    }
    
    // 二胡特有功能：拉弦（与拨弦不同）
    public void BowString(int stringIndex, float pressure)
    {
        if (stringIndex >= 0 && stringIndex < strings.Length && strings[stringIndex] != null)
        {
            // 拉弦逻辑
            strings[stringIndex].PluckString(); // 可以扩展 InstrumentString 添加 BowString 方法
            
            if (showDebug)
                Debug.Log($"🎻 拉弦: 弦 {stringIndex}, 压力: {pressure}");
        }
    }
}
```

2. 在场景中使用：
   - 为二胡模型添加 `ErhuController` 组件
   - 配置琴弦引用
   - 在 `InstrumentInputManager` 中添加二胡支持

---

### 5️⃣ 修改品位/徽位逻辑

#### 场景：修改古琴的徽位数量

**修改文件：** `GuqinController.cs`

**位置：** Inspector 面板或代码

```csharp
// 在 Inspector 中直接修改 numFrets 字段
[Header("古琴配置")]
public int numFrets = 13; // ← 改为你想要的数量，如 7、9、16 等
```

修改后，`CreateFretColliders()` 会自动创建对应数量的品位。

---

#### 场景：修改品位位置计算

**修改文件：** `PipaController.cs`

**位置：** `CalculateFretPosition()` 方法

```csharp
float CalculateFretPosition(int fretIndex)
{
    // 🔧 当前使用对数分布（符合物理规律）
    float ratio = Mathf.Pow(2f, -fretIndex / 12f);
    
    // 💡 示例1：线性分布（等距）
    // float ratio = 1f - (fretIndex / (float)numFrets);
    
    // 💡 示例2：自定义曲线
    // float ratio = Mathf.Lerp(1f, 0f, Mathf.Pow(fretIndex / (float)numFrets, 1.5f));
    
    return fingerboardStart + (fingerboardEnd - fingerboardStart) * ratio;
}
```

---

### 6️⃣ 添加力度检测

#### 场景：根据点击速度/压力改变音量

**修改文件：** `InstrumentInputManager.cs` 和 `InstrumentString.cs`

**步骤：**

1. 在 `InstrumentInputManager.cs` 添加力度计算：

```csharp
void OnInputDown(Vector2 screenPos)
{
    inputStartTime = Time.time;
    startInputPos = screenPos;
    isDragging = true;
    
    // ... 射线检测 ...
    
    if (Physics.Raycast(ray, out hit, 100f, raycastLayerMask))
    {
        lastHit = hit;
        
        // 🆕 计算点击速度（作为力度）
        float velocity = Input.GetAxis("Mouse ScrollDelta"); // 示例
        float strength = Mathf.Clamp01(velocity * 2f);
        
        if (hit.collider.CompareTag("InstrumentString"))
        {
            InstrumentString instrumentString = hit.collider.GetComponent<InstrumentString>();
            if (instrumentString != null)
            {
                instrumentString.PluckStringWithStrength(strength); // 🆕 新方法
            }
        }
    }
}
```

2. 在 `InstrumentString.cs` 添加新方法：

```csharp
public void PluckStringWithStrength(float strength)
{
    // ... 原有 PluckString 逻辑 ...
    
    if (clipToPlay != null && audioSource != null)
    {
        audioSource.clip = clipToPlay;
        
        // 🆕 根据力度调整音量
        audioSource.volume = Mathf.Lerp(0.3f, 1.0f, strength);
        
        audioSource.Play();
        
        Debug.Log($"🔊 播放音频，力度: {strength:F2}，音量: {audioSource.volume:F2}");
        
        // ... 后续代码 ...
    }
}
```

---

### 7️⃣ 添加音色切换

#### 场景：让同一根弦可以切换不同音色

**修改文件：** `InstrumentString.cs`

**步骤：**

1. 添加字段：

```csharp
[Header("音色设置")]
[Tooltip("可用的音色列表")]
public AudioClip[] timbres; // 不同音色的音频

private int currentTimbreIndex = 0;
```

2. 添加切换方法：

```csharp
public void SwitchTimbre(int index)
{
    if (index >= 0 && index < timbres.Length)
    {
        currentTimbreIndex = index;
        openStringSound = timbres[index];
        
        Debug.Log($"🎨 切换音色: {timbres[index].name}");
    }
}

public void NextTimbre()
{
    currentTimbreIndex = (currentTimbreIndex + 1) % timbres.Length;
    SwitchTimbre(currentTimbreIndex);
}
```

3. 在 UI 中添加按钮调用 `NextTimbre()`

---

### 8️⃣ 添加录制和回放功能

**创建新脚本：** `InstrumentRecorder.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NoteEvent
{
    public float time;
    public int stringIndex;
    public int fretIndex;
}

public class InstrumentRecorder : MonoBehaviour
{
    public GuqinController guqin; // 或 PipaController
    
    private List<NoteEvent> recording = new List<NoteEvent>();
    private bool isRecording = false;
    private float recordStartTime;
    
    // 开始录制
    public void StartRecording()
    {
        recording.Clear();
        isRecording = true;
        recordStartTime = Time.time;
        Debug.Log("🔴 开始录制");
    }
    
    // 停止录制
    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"⏹️ 停止录制，共 {recording.Count} 个音符");
    }
    
    // 记录音符（在琴弦被拨动时调用）
    public void RecordNote(int stringIndex, int fretIndex)
    {
        if (!isRecording) return;
        
        NoteEvent note = new NoteEvent
        {
            time = Time.time - recordStartTime,
            stringIndex = stringIndex,
            fretIndex = fretIndex
        };
        
        recording.Add(note);
    }
    
    // 回放录制
    public void PlayRecording()
    {
        StartCoroutine(PlayRecordingCoroutine());
    }
    
    System.Collections.IEnumerator PlayRecordingCoroutine()
    {
        Debug.Log("▶️ 开始回放");
        float startTime = Time.time;
        
        foreach (NoteEvent note in recording)
        {
            // 等待到正确的时间点
            while (Time.time - startTime < note.time)
            {
                yield return null;
            }
            
            // 播放音符
            if (guqin != null)
            {
                InstrumentString str = guqin.GetString(note.stringIndex);
                if (str != null)
                {
                    str.PluckString();
                }
            }
        }
        
        Debug.Log("✅ 回放完成");
    }
}
```

**使用方法：**
1. 创建空物体 `Recorder`
2. 添加 `InstrumentRecorder` 组件
3. 将 GuqinController 拖入 `guqin` 字段
4. 在 `InstrumentString.OnStringPlucked` 事件中调用 `RecordNote()`

---

## 🎯 最佳实践

### ✅ 代码规范

1. **始终添加调试日志**
   ```csharp
   if (showDebug)
       Debug.Log($"🎵 你的功能：详细信息");
   ```

2. **使用 Tooltip 描述字段**
   ```csharp
   [Tooltip("这个字段的作用说明")]
   public float myValue = 1.0f;
   ```

3. **空值检查**
   ```csharp
   if (audioSource != null && clipToPlay != null)
   {
       // 执行操作
   }
   ```

4. **使用协程处理时间相关逻辑**
   ```csharp
   StartCoroutine(MyTimedEffect());
   ```

### ✅ 性能优化

1. **缓存组件引用**
   ```csharp
   // ❌ 不好：每次都查找
   GetComponent<AudioSource>().Play();
   
   // ✅ 好：缓存引用
   private AudioSource audioSource;
   void Start() {
       audioSource = GetComponent<AudioSource>();
   }
   audioSource.Play();
   ```

2. **使用对象池**（如果需要大量粒子特效）

3. **避免在 Update() 中频繁查找**
   ```csharp
   // ❌ 不好
   void Update() {
       GameObject.Find("MyObject"); // 每帧查找
   }
   
   // ✅ 好
   private GameObject myObject;
   void Start() {
       myObject = GameObject.Find("MyObject"); // 只查找一次
   }
   ```

---

## 📚 快速参考

### 我想修改... → 应该修改哪个脚本？

| 功能需求 | 修改脚本 |
|---------|---------|
| 琴弦颜色、震动效果 | `InstrumentString.cs` |
| 音频音量、淡出效果 | `InstrumentString.cs` |
| 点击、滑动、双击检测 | `InstrumentInputManager.cs` |
| 品位数量、位置 | `GuqinController.cs` / `PipaController.cs` |
| 添加新乐器 | 创建新的 `XXXController.cs`（参考现有） |
| 射线检测范围、层级 | `InstrumentInputManager.cs` |
| 音频生成算法 | `InstrumentAudioManager.cs` |
| 添加特效（粒子、光效） | `InstrumentString.cs` + Unity 组件 |
| UI 交互 | 创建新的 `InstrumentUIManager.cs` |

---

## 🧪 测试你的修改

1. **小步修改**：一次只改一个功能
2. **添加日志**：用 `Debug.Log()` 确认代码被执行
3. **使用调试工具**：运行 `InstrumentDebugger` 检查状态
4. **查看 Console**：所有错误和警告都会显示在这里
5. **频繁测试**：每次修改后立即测试

---

## 💡 示例：完整的功能添加流程

### 场景：添加"和弦模式"（同时拨动多根弦）

**步骤：**

1. **在 `GuqinController.cs` 添加方法：**

```csharp
public void PlayChord(int[] stringIndices)
{
    if (showDebug)
        Debug.Log($"🎼 播放和弦：{string.Join(", ", stringIndices)}");
    
    foreach (int index in stringIndices)
    {
        if (index >= 0 && index < strings.Length && strings[index] != null)
        {
            strings[index].PluckString();
        }
    }
}
```

2. **在 UI 中添加和弦按钮（新建 `ChordUIController.cs`）：**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class ChordUIController : MonoBehaviour
{
    public GuqinController guqin;
    public Button chordButton;
    
    void Start()
    {
        chordButton.onClick.AddListener(PlayCMajorChord);
    }
    
    void PlayCMajorChord()
    {
        // C 大调和弦：弦 0, 2, 4
        guqin.PlayChord(new int[] { 0, 2, 4 });
    }
}
```

3. **在 Unity 中：**
   - 创建 UI Button
   - 添加 `ChordUIController` 组件
   - 连接引用

4. **测试：**
   - 点击按钮
   - 观察是否同时播放多根弦
   - 查看 Console 日志

---

## 🚀 下一步

现在您已经掌握了修改和扩展系统的方法！

**建议练习：**
1. ✏️ 修改琴弦的震动强度
2. 🎨 添加一个新的触摸颜色
3. 🔊 调整音频淡出时间
4. 🎯 添加双击检测
5. 🎼 实现和弦播放

**遇到问题？**
- 查看 Console 的错误日志
- 使用 `InstrumentDebugger` 诊断
- 参考 `TROUBLESHOOTING_CN.md`

祝您开发愉快！🎵

