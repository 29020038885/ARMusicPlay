# 古琴琵琶交互系统 - 快速开始

## 🎯 5分钟快速设置

### 第一步：导入脚本（已完成 ✅）
所有必需的脚本都已创建在 `Assets/Scripts/` 文件夹中。

### 第二步：设置古琴

1. **在场景中找到古琴模型**
   - 路径：`Assets/model/guqin/qin.fbx`
   - 拖入场景（如果还没有的话）

2. **创建空对象用于设置**
   - 在 Hierarchy 中右键 → Create Empty
   - 命名为 "GuqinSetup"
   - 添加组件：`InstrumentSetupHelper`

3. **配置参数**
   - Instrument Type: **Guqin**
   - Instrument Model: 拖入场景中的古琴模型
   - Number Of Strings: **7**
   - 调整弦的位置参数（根据你的模型）：
     ```
     String Start Position: (-0.15, 0.02, 0.5)
     String End Position: (-0.15, 0.02, -0.5)
     String Spacing: 0.05
     String Thickness: 0.005
     ```

4. **点击"完整设置乐器"按钮**
   - 会自动创建7根弦
   - 会自动添加 GuqinController 组件

### 第三步：设置琵琶

1. **在场景中找到琵琶模型**
   - 路径：`Assets/model/pipa/fbx.fbx`
   - 拖入场景（如果还没有的话）

2. **创建空对象用于设置**
   - 在 Hierarchy 中右键 → Create Empty
   - 命名为 "PipaSetup"
   - 添加组件：`InstrumentSetupHelper`

3. **配置参数**
   - Instrument Type: **Pipa**
   - Instrument Model: 拖入场景中的琵琶模型
   - Number Of Strings: **4**
   - 调整弦的位置参数（根据你的模型）：
     ```
     String Start Position: (-0.1, 0.02, 0.4)
     String End Position: (-0.1, 0.02, -0.3)
     String Spacing: 0.04
     String Thickness: 0.005
     ```

4. **点击"完整设置乐器"按钮**

### 第四步：设置输入管理器

1. **创建输入管理器**
   - 在 Hierarchy 中右键 → Create Empty
   - 命名为 "InputManager"
   - 添加组件：`InstrumentInputManager`

2. **配置引用**
   - Input Camera: 拖入 Main Camera
   - Guqin Controller: 拖入古琴对象（会自动找到组件）
   - Pipa Controller: 拖入琵琶对象
   - Active Instrument: **0**（0=古琴，1=琵琶）

3. **配置参数**
   - Auto Detect Platform: ✅ 勾选
   - Show Debug Ray: ✅ 勾选（调试用）
   - Show Debug Info: ✅ 勾选（调试用）

### 第五步：设置音频管理器（可选）

1. **创建音频管理器**
   - 在 Hierarchy 中右键 → Create Empty
   - 命名为 "AudioManager"
   - 添加组件：`InstrumentAudioManager`

2. **配置**
   - Use Procedural Audio: ✅ 勾选（自动生成音频）
   - Show Debug Info: ✅ 勾选

### 第六步：测试

1. **运行游戏**
   - 点击 Play 按钮

2. **测试拨弦**
   - 用鼠标点击琴弦，应该能听到声音并看到弦变色

3. **测试按品拨弦**
   - 先按住品位区域（保持按住）
   - 再点击对应的弦

4. **切换乐器**
   - 鼠标右键点击切换古琴/琵琶

## 🎮 操作方式

### 编辑器/PC 模式（鼠标）
- **左键点击弦**: 拨动空弦
- **左键按住品位 + 点击弦**: 按品拨弦
- **右键点击**: 切换古琴/琵琶

### 移动设备模式（触摸）
- **轻触弦**: 拨动空弦
- **按住品位 + 触摸弦**: 按品拨弦
- **快速上下滑动**: 扫弦（琵琶）

## 📐 调整弦的位置

如果自动生成的弦位置不对，需要根据你的模型调整：

1. **找到弦的起点和终点**
   - 在 Scene 视图中观察模型
   - 记录琴弦应该开始和结束的位置

2. **调整参数**
   - 修改 String Start Position 和 String End Position
   - 调整 String Spacing（弦之间的间距）

3. **重新生成**
   - 删除之前生成的 "Strings" 对象
   - 再次点击"创建弦"按钮

## 🎨 弦材质设置

如果想让弦更明显：

1. **创建材质**
   - Project 窗口右键 → Create → Material
   - 命名为 "StringMaterial"
   - 设置颜色（推荐：金色或银色）

2. **应用到弦**
   - 在 InstrumentSetupHelper 中拖入 String Material 字段
   - 重新生成弦

## 🔧 常见问题

### Q: 点击没反应？
**A:** 检查：
1. InputManager 是否正确配置
2. 摄像机引用是否正确
3. 弦对象是否有 Collider

### Q: 没有声音？
**A:** 检查：
1. AudioManager 是否启用 "Use Procedural Audio"
2. 弦对象是否有 AudioSource 组件
3. 音量是否为0

### Q: 弦的位置不对？
**A:** 
1. 在 Scene 视图中查看弦的位置
2. 调整 String Start/End Position 参数
3. 删除 Strings 对象，重新生成

## 📱 发布到手机

1. **切换平台**
   - File → Build Settings
   - 选择 Android 或 iOS
   - Switch Platform

2. **测试触摸**
   - 在 InputManager 中手动勾选 "Use Touch Input"
   - 或者保持 "Auto Detect Platform" 勾选

3. **构建运行**
   - Build and Run

## 🎵 下一步

- 录制真实的古琴/琵琶音频替换程序生成的声音
- 添加 UI 界面切换乐器
- 添加录音和回放功能
- 集成手部追踪（使用现有的 HandLandmarkProvider）

## 💡 提示

- 先测试一个乐器（古琴），确认工作正常后再设置另一个
- 使用 Debug 选项查看详细日志
- 调整弦的位置可能需要多次尝试
- Scene 视图的 Gizmos 可以帮助查看射线和碰撞体

---

**有问题随时问！Good luck! 🎵**

