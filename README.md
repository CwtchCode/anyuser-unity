# AnyUser Unity SDK

Universal accessibility standard and Behavioral Interceptors for **Unity 2021.3+** (Built-in and Universal Render Pipeline).

Eliminate "Configuration Fatigue" by configuring screen shake dampening, hold-vs-toggle input, aim assistance, and colorblind shaders before Frame 1.

---

## 📦 Installation Options

### Method A: Unity Package Manager (UPM via Git URL)
1. Open your Unity project.
2. Navigate to **Window &rarr; Package Manager**.
3. Click the **`+`** icon &rarr; **Add package from git URL...**
4. Enter:
   ```
   https://github.com/CwtchCode/anyuser-unity.git
   ```

### Method B: Manual Installation
1. Copy the `sdk/unity/` folder into your project's `Packages/` or `Assets/` directory:
   ```
   YourUnityProject/
   └── Packages/
       └── com.anyuser.accessibility/
           ├── package.json
           └── Runtime/
               ├── AnyUser.asmdef
               ├── AnyUserParser.cs
               ├── AnyUserCameraShake.cs
               ├── AnyUserInput.cs
               └── Shaders/
                   └── ColorblindCompensation.shader
   ```

---

## 🚀 Quick Start Guide

### 1. Ingestion Singleton (`AnyUserParser`)
Create an empty GameObject in your bootstrap/initial scene, name it `AnyUser`, and attach `AnyUserParser`:
- It is marked `DontDestroyOnLoad` and persists across scenes.
- Automatically discovers `Application.persistentDataPath/profile.anyuser` or `StreamingAssets/profile.anyuser`.

```csharp
using AnyUser;
using UnityEngine;

public class PlayerBoot : MonoBehaviour
{
    private void Start()
    {
        // Access calibrated preferences anywhere:
        float uiScale = AnyUserParser.Instance.UiScale;
        float screenShake = AnyUserParser.Instance.ScreenShake;
        bool isToggle = AnyUserParser.Instance.ToggleInsteadOfHold;
        float aimAssist = AnyUserParser.Instance.AimAssistStrength;

        Debug.Log($"AnyUser profile loaded: UI Scale {uiScale}x, Screen Shake {screenShake}x");
    }
}
```

---

## 🎮 Behavioral Interceptor Recipes

### 1. Screen Shake Interceptor (`AnyUserCameraShake`)
Attach `AnyUserCameraShake` to your Main Camera:
```csharp
// When an explosion or impact happens:
Camera.main.GetComponent<AnyUserCameraShake>().AddTrauma(0.6f);

// If profile.vision.screen_shake is 0.0 (static safe), vibration is 100% eliminated!
```

### 2. Swap Hold for Toggle (`AnyUserInput`)
```csharp
void Update()
{
    // Automatically behaves as toggle or hold based on motor.toggle_instead_of_hold
    bool isSprinting = AnyUserInput.IsActionActive(
        "Sprint", 
        Input.GetKey(KeyCode.LeftShift), 
        Input.GetKeyDown(KeyCode.LeftShift)
    );

    if (isSprinting) {
        // Sprint movement logic
    }
}
```

### 3. Filter Tremor Double-Taps
```csharp
void Update()
{
    bool rawFirePressed = Input.GetButtonDown("Fire1");

    // Automatically absorbs accidental double-taps within motor.input_repeat_delay_ms
    if (AnyUserInput.IsActionJustPressedDebounced("Fire1", rawFirePressed))
    {
        FireWeapon();
    }
}
```

### 4. Aim Assistance Magnetism
```csharp
Vector3 rawAim = Camera.main.transform.forward;
Vector3 assistedAim = AnyUserInput.CalculateAimAssist(
    transform.position,
    rawAim,
    enemyPositionsArray,
    maxDistance: 25f,
    maxAngleDeg: 30f
);

// Fire bullet along assistedAim vector
```

---

## 📜 Canonical Schema
Draft-07 specification:
[https://anyuser.net/schema/v1/anyuser-schema.json](https://anyuser.net/schema/v1/anyuser-schema.json)
