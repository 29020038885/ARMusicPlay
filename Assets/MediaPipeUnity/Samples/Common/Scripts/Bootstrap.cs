// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Threading;
using UnityEngine;

namespace Mediapipe.Unity.Sample
{
  public class Bootstrap : MonoBehaviour
  {
    private const string GlogInitializedKey = "MPUnity_GlogInitialized_v1";

    [SerializeField] private AppSettings _appSettings;

    public InferenceMode inferenceMode { get; private set; }
    public bool isFinished { get; private set; }
    private bool _isGlogInitialized;

    // MediaPipe 的 GLog 初始化是进程级别的（全局单例）。
    // 场景切换时如果存在多个 Bootstrap 实例，可能会重复调用 InitGoogleLogging() 导致直接致命崩溃。
    // 使用 Interlocked 做原子互斥，避免多个协程在同一帧几乎同时进入导致绕过 bool 防护。
    private static int _globalMediaPipeInitializedInt;

    private static bool IsGlogPersistentlyInitialized()
    {
      return PlayerPrefs.GetInt(GlogInitializedKey, 0) == 1;
    }

    private static void SetGlogPersistentlyInitialized(bool value)
    {
      PlayerPrefs.SetInt(GlogInitializedKey, value ? 1 : 0);
      PlayerPrefs.Save();
    }

    private void OnEnable()
    {
      var _ = StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
      Debug.Log("The configuration for the sample app can be modified using AppSettings.asset.");
#if !DEBUG && !DEVELOPMENT_BUILD
      Debug.LogWarning("Logging for the MediaPipeUnityPlugin will be suppressed. To enable logging, please check the 'Development Build' option and build.");
#endif

      if (_appSettings == null)
      {
        Debug.LogError("Bootstrap: _appSettings is not assigned. HandLandmarkerRunner will not be able to initialize ImageSource/AssetLoader. " +
                       "Fix: assign 'AppSettings.asset' to Bootstrap, or use a custom bootstrapper to initialize MediaPipe on mobile.");
        yield break;
      }

      // 抢占初始化权：只有第一个进入的协程会继续执行初始化逻辑。
      if (Interlocked.CompareExchange(ref _globalMediaPipeInitializedInt, 1, 0) != 0)
      {
        isFinished = true;
        yield break;
      }

      DecideInferenceMode();

      Logger.MinLogLevel = _appSettings.logLevel;

      Protobuf.SetLogHandler(Protobuf.DefaultLogHandler);

      Debug.Log("Setting global flags...");
      _appSettings.ResetGlogFlags();

      // Editor Stop/Play 会触发托管域重载，静态互斥无法跨域维持。
      // 通过 PlayerPrefs 做“跨域标记”，避免在 native 层已初始化时重复 InitGoogleLogging() 导致致命崩溃。
      bool alreadyInited = IsGlogPersistentlyInitialized();
      if (!alreadyInited)
      {
        Glog.Initialize("MediaPipeUnityPlugin");
        _isGlogInitialized = true;
        SetGlogPersistentlyInitialized(true);
      }
      else
      {
        _isGlogInitialized = false;
      }

      Debug.Log("Initializing AssetLoader...");
      switch (_appSettings.assetLoaderType)
      {
        case AppSettings.AssetLoaderType.AssetBundle:
          {
            AssetLoader.Provide(new AssetBundleResourceManager("mediapipe"));
            break;
          }
        case AppSettings.AssetLoaderType.StreamingAssets:
          {
            AssetLoader.Provide(new StreamingAssetsResourceManager());
            break;
          }
        case AppSettings.AssetLoaderType.Local:
          {
#if UNITY_EDITOR
            AssetLoader.Provide(new LocalResourceManager());
            break;
#else
            Debug.LogError("LocalResourceManager is only supported on UnityEditor." +
              "To avoid this error, consider switching to the StreamingAssetsResourceManager and copying the required resources under StreamingAssets, for example.");
            yield break;
#endif
          }
        default:
          {
            Debug.LogError($"AssetLoaderType is unknown: {_appSettings.assetLoaderType}");
            yield break;
          }
      }

      if (inferenceMode == InferenceMode.GPU)
      {
        Debug.Log("Initializing GPU resources...");
        yield return GpuManager.Initialize();

        if (!GpuManager.IsInitialized)
        {
          Debug.LogWarning("If your native library is built for CPU, change 'Preferable Inference Mode' to CPU from the Inspector Window for AppSettings");
        }
      }

      Debug.Log("Preparing ImageSource...");
      ImageSourceProvider.Initialize(
        _appSettings.BuildWebCamSource(), _appSettings.BuildStaticImageSource(), _appSettings.BuildVideoSource());
      ImageSourceProvider.Switch(_appSettings.defaultImageSource);

      isFinished = true;
    }

    private void OnDestroy()
    {
      // 不在这里做 shutdown：Editor Stop/Play 的生命周期复杂，shutdown 可能触发
      // “ShutdownGoogleLogging() without calling InitGoogleLogging() first” 之类的致命错误。
      // 我们通过 PlayerPrefs 跳过重复 Init 来避免崩溃；shutdown 交给 OnApplicationQuit。
      _isGlogInitialized = false;
    }

    private void DecideInferenceMode()
    {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
      if (_appSettings.preferableInferenceMode == InferenceMode.GPU) {
        Debug.LogWarning("Current platform does not support GPU inference mode, so falling back to CPU mode");
      }
      inferenceMode = InferenceMode.CPU;
#else
      inferenceMode = _appSettings.preferableInferenceMode;
#endif
    }

    private void OnApplicationQuit()
    {
      bool inited = IsGlogPersistentlyInitialized();

      if (inited)
      {
        try { GpuManager.Shutdown(); } catch { /* ignore */ }
        try { Glog.Shutdown(); } catch { /* ignore */ }
        try { Protobuf.ResetLogHandler(); } catch { /* ignore */ }

        SetGlogPersistentlyInitialized(false);
      }
    }
  }
}
