# BLACKZONE — Android Build Guide (Windows 11)

Target: **Android ARM64 APK**, Unity 6000.0.82f1, URP, landscape.

## 1. Install the toolchain (one time)

1. **Unity Hub** → *Installs* → **Unity 6000.0.82f1 LTS** → add modules:
   - **Android Build Support**
   - **Android SDK & NDK Tools**
   - **OpenJDK**
   (Unity installs its own SDK/NDK/JDK into `%ProgramFiles%\Unity\Hub\Editor\...`.
   Do **not** install separate Android Studio toolchains unless you know why.)
2. Verify in Unity: `Edit ▸ Preferences ▸ External Tools ▸ Android` — check the
   SDK/NDK/JDK paths are detected (green).

## 2. One-time project configuration (in Unity Editor)

After the first open (and compilation), run these menus once:

| Menu | What it does |
| ---- | ------------ |
| **Blackzone ▸ 01 - Create URP Asset + Quality Levels** | creates `Assets/_Blackzone/Settings/URP/BlackzoneURP.asset`, assigns it as the default render pipeline and to quality levels |
| **Blackzone ▸ 02 - Configure Android Player Settings** | package id `com.blackzone.tactical`, product `BLACKZONE`, landscape, ARM64, IL2CPP, minSdk 24, targetSdk auto, strips engine code, switches active target to Android |
| **Blackzone ▸ 03 - Create Weapon + AI Data Assets** | generates `Resources/Weapons/*.asset` + `Resources/AI/*.asset` from the code catalogs (optional — the game has code fallbacks) |

## 3. Build the APK

1. `File ▸ Build Settings` → platform **Android** → scene
   `Assets/_Blackzone/Scenes/Blackzone_Phase1.unity` should be listed & checked.
2. `Player Settings ▸ Other Settings` sanity check:
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64** only
   - Minimum API Level: **24**
   - Internet Access: Auto (not required by the game)
   - Graphics APIs: leave **OpenGLES3 / Vulkan** order as Unity picked; do not
     remove Vulkan (used on modern devices), OpenGLES3 covers older ones.
3. Click **Build** → output e.g. `Builds/Blackzone.apk`.

## 4. Install on a device (USB)

1. Enable **Developer options** on the phone (Settings → About → tap
   Build number 7×).
2. Enable **USB debugging** (Developer options).
3. Connect via USB; accept the RSA fingerprint dialog on the phone.
4. Open a terminal in `%LOCALAPPDATA%\Android\Sdk\platform-tools`
   (or add `adb` to PATH) and run:

```bat
adb devices          REM confirm the device shows as "device"
adb install -r Builds\Blackzone.apk
adb shell am start -n com.blackzone.tactical/com.unity3d.player.UnityPlayerActivity
```

5. Logcat (optional diagnostics):

```bat
adb logcat -s Unity
```

## 5. Notes

- **Keystore / signing:** for a personal test build, `Player Settings ▸
  Publishing Settings ▸ Keystore Manager` → create a keystore and password.
  **Never commit the keystore or passwords to the repository.**
- **Frame rate:** the quality preset (LOW/MEDIUM/HIGH) sets the FPS cap
  (30/45/60). Use the in-game FPS counter (top-left, dev builds) to measure.
- **Performance test flow:** LOW on an old device, MEDIUM/HIGH on newer ones;
  note actual measured FPS (never invented numbers).
- **iOS** is a later phase; nothing in the code blocks it, but no build
  project has been prepared yet.

## 6. Common problems

| Symptom | Fix |
| ------- | --- |
| “No Android module installed” | add Android Build Support + SDK/NDK/OpenJDK in Unity Hub |
| Gradle fails with Java errors | make sure Unity's bundled OpenJDK is selected in Preferences |
| `adb: device unauthorized` | re-accept the RSA prompt; replug USB |
| Black screen with log spam | check logcat `Unity` tag; report with logs |
| RenderPipeline not assigned error | run menu **Blackzone ▸ 01** |
