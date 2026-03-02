# 古琴和琵琶弹奏交互系统 - 使用指南

## 📋 系统概述

这是一个完整的中国传统乐器（古琴和琵琶）交互系统，支持：
- ✅ 触摸/点击琴弦发声
- ✅ 按住品位再拨弦产生不同音高
- ✅ 编辑器模式（鼠标）和移动设备模式（触摸）
- ✅ 程序化音频生成（无需音频文件即可使用）
- ✅ 扫弦手势支持（琵琶）

## 🎯 核心脚本说明

### 1. **InstrumentString.cs**
- 表示单根琴弦
- 处理拨弦、按品、音频播放
- 提供视觉反馈

### 2. **GuqinController.cs**
- 管理古琴的7根弦
- 处理品位检测
- 协调整体交互

### 3. **PipaController.cs**
- 管理琵琶的4根弦
- 支持24品位
- 支持扫弦手势

### 4. **InstrumentInputManager.cs**
- 统一输入管理（鼠标/触摸）
- 射线检测
- 手势识别（点击、长按、滑动）

### 5. **InstrumentAudioManager.cs**
- 音频资源管理
- 程序化音频生成
- 音频缓存

### 6. **InstrumentSetupHelper.cs**
- 编辑器辅助工具
- 快速创建弦和碰撞体
- 自动配置控制器

## 🚀 快速开始

### 方法一：使用设置助手（推荐）

1. **准备乐器模型**
   - 将古琴模型（qin.fbx）拖入场景
   - 将琵琶模型（fbx.fbx）拖入场景

2. **添加设置助手**
   - 在场景中创建空对象，命名为 "InstrumentSetup"
   - 添加 `InstrumentSetupHelper` 组件
   - 选择乐器类型（Guqin 或 Pipa）
   - 拖拽乐器模型到 "Instrument Model" 字段

3. **调整参数**
   ```
   古琴参数示例：
   - Number Of Strings: 7
   - String Start Position: (-0.15, 0.02, 0.5)
   - String End Position: (-0.15, 0.02, -0.5)
   - String Spacing: 0.05
   
   琵琶参数示例：
   - Number Of Strings: 4
   - String Start Position: (-0.1, 0.02, 0.4)
   - String End Position: (-0.1, 0.02, -0.3)
   - String Spacing: 0.04
   ```

4. **点击"完整设置乐器"按钮**
   - 自动创建弦对象
   - 自动添加碰撞体
   - 自动配置控制器

### 方法二：手动设置

1. **创建弦对象**
   ```
   乐器根对象
   └── Strings
       ├── String_0 (InstrumentString组件 + BoxCollider + LineRenderer)
       ├── String_1
       ├── String_2
       └── ...
   ```

2. **添加控制器**
   - 在古琴根对象上添加 `GuqinController` 组件
   - 在琵琶根对象上添加 `PipaController` 组件
   - 将弦对象拖入 Strings 数组

3. **配置输入管理器**
   - 创建空对象命名为 "InputManager"
   - 添加 `InstrumentInputManager` 组件
   - 拖拽古琴和琵琶控制器到对应字段

4. **配置音频管理器（可选）**
   - 创建空对象命名为 "AudioManager"
   - 添加 `InstrumentAudioManager` 组件
   - 启用 "Use Procedural Audio" 即可自动生成音频

## 🎮 使用方法

### 编辑器模式（鼠标）

1. **拨动空弦**
   - 直接点击琴弦

2. **按品位拨弦**
   - 在品位位置按住鼠标左键
   - 保持按住，点击对应的弦

3. **切换乐器**
   - 右键点击切换古琴/琵琶

### 移动设备模式（触摸）

1. **拨动空弦**
   - 轻触琴弦

2. **按品位拨弦**
   - 手指按住品位
   - 用另一只手指触碰对应的弦

3. **扫弦（仅琵琶）**
   - 在琴弦区域快速上下滑动

## 📁 文件夹结构

```
Assets/
├── Scripts/
│   ├── InstrumentString.cs
│   ├── GuqinController.cs
│   ├── PipaController.cs
│   ├── InstrumentInputManager.cs
│   ├── InstrumentAudioManager.cs
│   └── InstrumentSetupHelper.cs
├── model/
│   ├── guqin/
│   │   └── qin.fbx
│   └── pipa/
│       └── fbx.fbx
└── Resources/ (可选，用于存放音频)
    └── Audio/
        ├── Guqin/
        │   ├── string0_fret-1.wav
        │   ├── string0_fret0.wav
        │   └── ...
        └── Pipa/
            ├── string0_fret-1.wav
            └── ...
```

## 🎵 音频配置

### 选项1：使用程序化音频（无需准备音频文件）

- 在 `InstrumentAudioManager` 中启用 "Use Procedural Audio"
- 系统会自动生成各弦各品的音频
- 可调整音色参数（ADSR包络、谐波）

### 选项2：使用自定义音频文件

1. 创建 `Resources/Audio/Guqin` 文件夹
2. 添加音频文件，命名格式：
   - `string0_fret-1.wav` （第0弦空弦）
   - `string0_fret0.wav` （第0弦第0品）
   - `string1_fret2.wav` （第1弦第2品）

## 🔧 参数调整

### 古琴设置
- **弦数量**: 7根
- **品位数量**: 13个徽位
- **音域**: C4-B4 (MIDI 60-71)

### 琵琶设置
- **弦数量**: 4根
- **品位数量**: 24品
- **音域**: D3-D4 (MIDI 50-62)

### 输入管理器
- **Long Press Threshold**: 长按判定时间（默认0.2秒）
- **Swipe Threshold**: 滑动判定距离（默认50像素）
- **Active Instrument**: 当前激活的乐器（0=古琴，1=琵琶）

## 🐛 故障排除

### 问题1：点击没有反应
- ✅ 检查 InstrumentInputManager 是否正确配置
- ✅ 确认弦对象有 Collider 组件
- ✅ 检查摄像机是否正确设置

### 问题2：没有声音
- ✅ 确认弦对象有 AudioSource 组件
- ✅ 检查 InstrumentAudioManager 是否启用程序化音频
- ✅ 确认音量设置不为0

### 问题3：品位检测不准
- ✅ 调整 GuqinController/PipaController 的品位碰撞体位置
- ✅ 使用 Scene 视图查看碰撞体是否正确放置

## 📱 发布到手机

1. **切换平台**
   - File → Build Settings
   - 选择 Android 或 iOS
   - 点击 "Switch Platform"

2. **配置输入**
   - InstrumentInputManager 会自动检测平台
   - 或手动启用 "Use Touch Input"

3. **构建**
   - 点击 "Build and Run"

## 🎨 自定义

### 修改音色
在 `InstrumentAudioManager` 中调整：
- Attack Time（起音时间）
- Decay Time（衰减时间）
- Sustain Level（持续电平）
- Release Time（释放时间）
- Harmonics（谐波比例）

### 修改弦的视觉效果
在 `InstrumentString` 中调整：
- Touch Color（触碰时的颜色）
- Default Color（默认颜色）
- Vibration Duration（振动持续时间）

## 📞 技术支持

如有问题，请检查：
1. Unity Console 中的错误信息
2. 开启各脚本的 "Show Debug" 选项查看详细日志
3. 确认所有组件引用都已正确配置

## 🎼 音符对照表

### 古琴七弦（由粗到细）
1. 一弦：C4 (MIDI 60)
2. 二弦：D4 (MIDI 62)
3. 三弦：E4 (MIDI 64)
4. 四弦：F4 (MIDI 65)
5. 五弦：G4 (MIDI 67)
6. 六弦：A4 (MIDI 69)
7. 七弦：B4 (MIDI 71)

### 琵琶四弦（由粗到细）
1. 一弦：D3 (MIDI 50)
2. 二弦：G3 (MIDI 55)
3. 三弦：B3 (MIDI 59)
4. 四弦：D4 (MIDI 62)

---

**祝你使用愉快！🎵**

