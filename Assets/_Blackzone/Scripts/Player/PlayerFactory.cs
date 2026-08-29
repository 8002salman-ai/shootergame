using Blackzone.Combat;
using Blackzone.Utilities;
using Blackzone.Weapons;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Blackzone.Player
{
    /// <summary>
    /// Composes the first-person rig at runtime: capsule, movement, health,
    /// armor, camera + look, viewmodel and the weapon arsenal.
    /// </summary>
    public static class PlayerFactory
    {
        public sealed class PlayerRig
        {
            public Transform Root;
            public Transform CameraPivot;
            public Camera Camera;
            public FpsMovement Movement;
            public FpsLook Look;
            public Health Health;
            public Armor Armor;
            public WeaponArsenal Arsenal;
        }

        public static PlayerRig Build(Transform parent, Vector3 spawnPosition)
        {
            var rig = new PlayerRig();

            var rootGo = new GameObject("PlayerRig");
            rootGo.layer = GameConstants.LayerPlayer;
            rootGo.transform.SetParent(parent, false);
            rootGo.transform.position = spawnPosition;
            rig.Root = rootGo.transform;

            // Capsule
            var cc = rootGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.08f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;

            // Combat stats
            rig.Health = rootGo.AddComponent<Health>();
            rig.Health.Initialize(GameConstants.PlayerMaxHealth);
            rig.Armor = rootGo.AddComponent<Armor>();
            rig.Armor.Initialize(GameConstants.PlayerArmorCapacity, GameConstants.PlayerArmorAbsorb);

            // Movement (creates CameraPivot child)
            rig.Movement = rootGo.AddComponent<FpsMovement>();
            rig.CameraPivot = rig.Movement.CameraPivot;

            // Camera
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(rig.CameraPivot, false);
            camGo.layer = 0;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = GameConstants.BaseFov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 320f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.55f, 0.53f, 0.48f); // fallback if no skybox
            cam.cullingMask = ~(1 << GameConstants.LayerUI);
            rig.Camera = cam;
            rig.Look = camGo.AddComponent<FpsLook>();

            // Enable URP post-processing on camera
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // Viewmodel root (child of camera)
            var viewRoot = new GameObject("ViewmodelRoot").transform;
            viewRoot.SetParent(cam.transform, false);
            viewRoot.localPosition = Vector3.zero;
            viewRoot.localRotation = Quaternion.identity;

            // Arsenal
            rig.Arsenal = rootGo.AddComponent<WeaponArsenal>();
            var defs = WeaponCatalog.GetWeaponDefinitions();
            rig.Arsenal.Initialize(defs, viewRoot, cam, rig.Look, rig.Movement);

            return rig;
        }
    }
}
