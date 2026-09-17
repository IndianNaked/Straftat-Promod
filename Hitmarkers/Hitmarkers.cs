using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

public static class HitmarkerManager
{
    private static HitmarkerController instance;

    public static void Play(bool headshot, bool kill)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return;
        }
        if (Crosshair.Instance == null)
        {
            return;
        }
        VanillaHitmarkerCleaner.Ensure();
        EnsureInstance();
        instance.Play(headshot, kill, false);
    }

    public static void PlayBarrel(bool kill)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return;
        }
        if (!HitmarkerConfig.EnableBarrelHitmarker.Value)
        {
            return;
        }
        if (Crosshair.Instance == null)
        {
            return;
        }
        VanillaHitmarkerCleaner.Ensure();
        EnsureInstance();
        instance.Play(false, kill, true);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }
        var go = new GameObject("CustomHitmarker");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(Crosshair.Instance.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        instance = go.AddComponent<HitmarkerController>();
        instance.Initialize();
    }

}

public sealed class VanillaHitmarkerCleaner : MonoBehaviour
{
    private static VanillaHitmarkerCleaner instance;
    private static readonly string[] MarkerNameHints = { "HitMarker", "KillMarker" };

    public static void Ensure()
    {
        if (instance != null)
        {
            return;
        }
        if (PauseManager.Instance == null)
        {
            return;
        }
        var go = new GameObject("VanillaHitmarkerCleaner");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(PauseManager.Instance.transform, false);
        instance = go.AddComponent<VanillaHitmarkerCleaner>();
    }

    private void LateUpdate()
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value || !HitmarkerConfig.HideVanillaHitmarkers.Value)
        {
            return;
        }
        if (PauseManager.Instance == null)
        {
            return;
        }

        Transform parent = PauseManager.Instance.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }
            if (LooksLikeVanillaHitmarker(child.gameObject))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static bool LooksLikeVanillaHitmarker(GameObject obj)
    {
        string name = obj.name;
        for (int i = 0; i < MarkerNameHints.Length; i++)
        {
            if (name.IndexOf(MarkerNameHints[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class HitmarkerController : MonoBehaviour
{
    private sealed class Segment
    {
        public RectTransform root;
        public RectTransform outlineRect;
        public RectTransform innerRect;
        public Image outlineImage;
        public Image innerImage;
        public float angle;
    }

    private readonly List<Segment> segments = new List<Segment>(4);
    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private HitmarkerShape currentShape;
    private float elapsed;
    private bool active;

    private const float DisplaySeconds = 0.18f;
    private const float StartScale = 1.18f;

    public void Initialize()
    {
        rect = transform as RectTransform;
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        currentShape = (HitmarkerShape)(-1);
        ApplyConfig();
        gameObject.SetActive(false);
    }

    public void Play(bool headshot, bool kill, bool barrel)
    {
        ApplyConfig();
        ApplyColors(headshot, kill, barrel);

        elapsed = 0f;
        active = true;
        canvasGroup.alpha = 1f;
        if (rect != null)
        {
            rect.localScale = Vector3.one * StartScale;
        }
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / DisplaySeconds);
        float fade = t * t;
        canvasGroup.alpha = 1f - fade;
        if (rect != null)
        {
            float scale = Mathf.Lerp(StartScale, 1f, Mathf.Clamp01(t * 2.2f));
            rect.localScale = Vector3.one * scale;
        }
        if (t >= 1f)
        {
            active = false;
            gameObject.SetActive(false);
        }
    }

    private void ApplyConfig()
    {
        if (currentShape != HitmarkerConfig.Shape.Value)
        {
            BuildSegments(HitmarkerConfig.Shape.Value);
        }

        int length = HitmarkerConfig.GetLength();
        int thickness = HitmarkerConfig.GetThickness();
        int gap = HitmarkerConfig.GetGap();
        bool outlineEnabled = HitmarkerConfig.EnableOutline.Value;
        int outlineThickness = outlineEnabled ? HitmarkerConfig.GetOutlineThickness() : 0;

        for (int i = 0; i < segments.Count; i++)
        {
            Segment seg = segments[i];
            float rad = seg.angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float offset = gap + (length * 0.5f);
            seg.root.anchoredPosition = dir * offset;
            seg.root.localRotation = Quaternion.Euler(0f, 0f, seg.angle);

            seg.innerRect.sizeDelta = new Vector2(length, thickness);
            seg.outlineRect.sizeDelta = new Vector2(length + outlineThickness * 2, thickness + outlineThickness * 2);
            seg.outlineImage.enabled = outlineEnabled && outlineThickness > 0;
        }
    }

    private void ApplyColors(bool headshot, bool kill, bool barrel)
    {
        Color inner = kill
            ? HitmarkerConfig.KillColor.Value
            : (barrel ? HitmarkerConfig.BarrelColor.Value : (headshot ? HitmarkerConfig.HeadshotColor.Value : HitmarkerConfig.Color.Value));
        Color outline = kill
            ? HitmarkerConfig.KillOutlineColor.Value
            : (barrel ? HitmarkerConfig.BarrelOutlineColor.Value : (headshot ? HitmarkerConfig.HeadshotOutlineColor.Value : HitmarkerConfig.OutlineColor.Value));
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].innerImage.color = inner;
            segments[i].outlineImage.color = outline;
        }
    }

    private void BuildSegments(HitmarkerShape shape)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        segments.Clear();

        float[] angles = shape == HitmarkerShape.Plus
            ? new[] { 0f, 90f, 180f, 270f }
            : new[] { 45f, 135f, 225f, 315f };

        for (int i = 0; i < angles.Length; i++)
        {
            segments.Add(CreateSegment(angles[i]));
        }

        currentShape = shape;
    }

    private Segment CreateSegment(float angle)
    {
        var rootGo = new GameObject("Segment");
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.SetParent(transform, false);
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);

        var outlineGo = new GameObject("Outline");
        var outlineRt = outlineGo.AddComponent<RectTransform>();
        outlineRt.SetParent(rootRt, false);
        outlineRt.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRt.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRt.pivot = new Vector2(0.5f, 0.5f);
        var outlineImg = outlineGo.AddComponent<Image>();
        outlineImg.raycastTarget = false;

        var innerGo = new GameObject("Inner");
        var innerRt = innerGo.AddComponent<RectTransform>();
        innerRt.SetParent(rootRt, false);
        innerRt.anchorMin = new Vector2(0.5f, 0.5f);
        innerRt.anchorMax = new Vector2(0.5f, 0.5f);
        innerRt.pivot = new Vector2(0.5f, 0.5f);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.raycastTarget = false;

        return new Segment
        {
            root = rootRt,
            outlineRect = outlineRt,
            innerRect = innerRt,
            outlineImage = outlineImg,
            innerImage = innerImg,
            angle = angle
        };
    }
}

internal static class HitmarkerEvents
{
    private static readonly Dictionary<Type, FieldInfo> enemyHealthFieldByType = new Dictionary<Type, FieldInfo>(8);
    private static int lastFrame = -1;
    private static int lastTargetId;
    private static int lastPriority = -1;

    public static void PlayWeaponDamage(Weapon weapon, float damageToGive, PlayerHealth target, string hitName)
    {
        if (!CanShowWeaponHit(weapon, target))
        {
            return;
        }

        bool headshot = IsHeadshot(hitName);
        bool kill = IsKill(target, damageToGive);
        PlayForTarget(target, headshot, kill, false);
        ClearVanillaMarker(weapon);
    }

    public static void PlayWeaponKill(Weapon weapon, PlayerHealth target)
    {
        if (!CanShowWeaponHit(weapon, target))
        {
            return;
        }

        PlayForTarget(target, false, true, false);
        ClearVanillaMarker(weapon);
    }

    public static void PlayWeaponContact(Weapon weapon, PlayerHealth target)
    {
        if (!CanShowWeaponHit(weapon, target))
        {
            return;
        }

        PlayForTarget(target, false, false, false);
        ClearVanillaMarker(weapon);
    }

    public static void PlayWeaponMarker(Weapon weapon)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return;
        }
        if (!IsLocalWeapon(weapon))
        {
            return;
        }

        var marker = weapon.marker;
        if (marker == null)
        {
            return;
        }

        PlayerHealth target = GetEnemyHealth(weapon);
        if (IsLocalTarget(target))
        {
            ClearVanillaMarker(weapon);
            return;
        }

        bool headshot = IsHeadshotMarker(marker);
        bool kill = target != null && IsKill(target, 0f);
        PlayForTarget(target, headshot, kill, false);
        ClearVanillaMarker(weapon);
    }

    public static void PlayProjectileHit(bool headshot)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return;
        }

        PlayForTarget(null, headshot, false, false);
    }

    public static void PlayPhysicsPropHit(PlayerHealth target, bool kill)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value || !HitmarkerConfig.EnableBarrelHitmarker.Value)
        {
            return;
        }
        if (IsLocalTarget(target))
        {
            return;
        }

        PlayForTarget(target, false, kill, true);
    }

    public static void PlayRemoveHealthFallback(PlayerHealth target, float beforeHealth, float damage)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return;
        }
        if (target == null || damage <= 0f || IsLocalTarget(target))
        {
            return;
        }

        float afterHealth = target.sync___get_value_health();
        if (afterHealth >= beforeHealth)
        {
            return;
        }
        if (!TargetHasLocalKiller(target))
        {
            return;
        }

        bool kill = target.sync___get_value_isKilled() || afterHealth <= 0f || beforeHealth - damage <= 0f;
        PlayForTarget(target, false, kill, false);
    }

    public static bool ShouldWatchPhysicsProp(PhysicsProp prop)
    {
        if (prop == null)
        {
            return false;
        }
        if (PhysicsPropHitmarkerTracker.IsTracked(prop))
        {
            return true;
        }
        if (prop.IsOwner)
        {
            return true;
        }

        FirstPersonController holder = prop.sync___get_value_playerThatHasGrabbedBarrel();
        return holder != null && holder.IsOwner;
    }

    private static bool CanShowWeaponHit(Weapon weapon, PlayerHealth target)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return false;
        }
        if (!IsLocalWeapon(weapon))
        {
            return false;
        }
        return target != null && !IsLocalTarget(target);
    }

    private static bool IsLocalWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            return false;
        }
        if (weapon.IsOwner)
        {
            return true;
        }

        FirstPersonController controller = weapon.playerController;
        if (controller != null && controller.IsOwner)
        {
            return true;
        }

        Transform root = weapon.transform != null ? weapon.transform.root : null;
        controller = root != null ? root.GetComponent<FirstPersonController>() : null;
        return controller != null && controller.IsOwner;
    }

    private static bool IsLocalTarget(PlayerHealth target)
    {
        if (target == null)
        {
            return false;
        }
        if (target.IsOwner)
        {
            return true;
        }

        FirstPersonController controller = target.controller;
        if (controller == null)
        {
            controller = target.GetComponent<FirstPersonController>();
        }
        return controller != null && controller.IsOwner;
    }

    private static bool TargetHasLocalKiller(PlayerHealth target)
    {
        Transform killer = target.sync___get_value_killer();
        if (killer == null)
        {
            return false;
        }

        FirstPersonController controller = killer.GetComponent<FirstPersonController>();
        if (controller == null)
        {
            controller = killer.GetComponentInParent<FirstPersonController>();
        }
        return controller != null && controller.IsOwner;
    }

    private static bool IsHeadshot(string hitName)
    {
        return string.Equals(hitName, "Head_Col", StringComparison.Ordinal)
            || string.Equals(hitName, "Neck_1_Col", StringComparison.Ordinal);
    }

    private static bool IsKill(PlayerHealth target, float damageToGive)
    {
        if (target == null)
        {
            return false;
        }

        float health = target.sync___get_value_health();
        return target.sync___get_value_isKilled() || health <= 0f || (damageToGive > 0f && health - damageToGive <= 0f);
    }

    private static void PlayForTarget(PlayerHealth target, bool headshot, bool kill, bool barrel)
    {
        int targetId = target != null ? target.GetInstanceID() : 0;
        int priority = kill ? 3 : (headshot ? 2 : (barrel ? 1 : 0));
        if (!ShouldPlay(targetId, priority))
        {
            return;
        }

        if (barrel)
        {
            HitmarkerManager.PlayBarrel(kill);
        }
        else
        {
            HitmarkerManager.Play(headshot, kill);
        }
    }

    private static bool ShouldPlay(int targetId, int priority)
    {
        int frame = Time.frameCount;
        if (lastFrame == frame && lastTargetId == targetId && priority <= lastPriority)
        {
            return false;
        }

        lastFrame = frame;
        lastTargetId = targetId;
        lastPriority = priority;
        return true;
    }

    private static bool IsHeadshotMarker(GameObject marker)
    {
        var image = marker.GetComponent<Image>();
        if (image == null)
        {
            return false;
        }
        Color c = image.color;
        return c.r > 0.9f && c.g < 0.2f && c.b < 0.2f;
    }

    private static PlayerHealth GetEnemyHealth(Weapon weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        Type type = weapon.GetType();
        FieldInfo field;
        if (!enemyHealthFieldByType.TryGetValue(type, out field))
        {
            field = AccessTools.Field(type, "enemyHealth");
            enemyHealthFieldByType[type] = field;
        }
        return field != null ? field.GetValue(weapon) as PlayerHealth : null;
    }

    private static void ClearVanillaMarker(Weapon weapon)
    {
        if (weapon == null || !HitmarkerConfig.HideVanillaHitmarkers.Value)
        {
            return;
        }
        if (weapon.marker != null)
        {
            UnityEngine.Object.Destroy(weapon.marker);
            weapon.marker = null;
        }
    }
}

internal static class PhysicsPropHitmarkerTracker
{
    private static readonly Dictionary<PhysicsProp, float> trackedProps = new Dictionary<PhysicsProp, float>();
    private const float TrackSeconds = 4f;

    public static void Mark(PhysicsProp prop, Transform player)
    {
        if (prop == null || !IsLocalPlayer(player))
        {
            return;
        }

        trackedProps[prop] = Time.time + TrackSeconds;
    }

    public static bool IsTracked(PhysicsProp prop)
    {
        if (prop == null)
        {
            return false;
        }

        float until;
        if (!trackedProps.TryGetValue(prop, out until))
        {
            return false;
        }
        if (Time.time <= until)
        {
            return true;
        }

        trackedProps.Remove(prop);
        return false;
    }

    private static bool IsLocalPlayer(Transform player)
    {
        if (player == null)
        {
            return false;
        }

        FirstPersonController controller = player.GetComponent<FirstPersonController>();
        if (controller == null)
        {
            controller = player.GetComponentInParent<FirstPersonController>();
        }
        return controller != null && controller.IsOwner;
    }
}

internal static class HitmarkerPatchUtils
{
    private static readonly Dictionary<Type, FieldInfo> audioFieldByType = new Dictionary<Type, FieldInfo>(3);
    private static readonly Dictionary<Type, FieldInfo> sfxFieldByType = new Dictionary<Type, FieldInfo>(3);

    public static bool Handle(object instance, bool headshot)
    {
        if (!HitmarkerConfig.EnableCustomHitmarkers.Value)
        {
            return true;
        }

        HitmarkerEvents.PlayProjectileHit(headshot);

        if (!HitmarkerConfig.HideVanillaHitmarkers.Value)
        {
            return true;
        }

        PlayVanillaAudio(instance, headshot);
        return false;
    }

    private static void PlayVanillaAudio(object instance, bool headshot)
    {
        if (instance == null)
        {
            return;
        }
        var type = instance.GetType();
        var audioField = GetFieldCached(audioFieldByType, type, "audio");
        var sfxField = GetFieldCached(sfxFieldByType, type, "hitSfx");
        var audio = audioField != null ? audioField.GetValue(instance) as AudioSource : null;
        var sfx = sfxField != null ? sfxField.GetValue(instance) as AudioClip : null;
        if (audio != null && sfx != null)
        {
            audio.PlayOneShot(sfx);
        }
        if (headshot && Crosshair.Instance != null && Crosshair.Instance.headshotHitClip != null && audio != null)
        {
            audio.PlayOneShot(Crosshair.Instance.headshotHitClip);
        }
    }

    private static FieldInfo GetFieldCached(Dictionary<Type, FieldInfo> cache, Type type, string fieldName)
    {
        FieldInfo field;
        if (cache.TryGetValue(type, out field))
        {
            return field;
        }
        field = AccessTools.Field(type, fieldName);
        cache[type] = field;
        return field;
    }
}

internal static class HitmarkerWeaponPatchUtils
{
    public static void HandleWeaponMarker(Weapon weapon)
    {
        HitmarkerEvents.PlayWeaponMarker(weapon);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Weapon), "CmdDamageProp")]
public static class Weapon_BarrelHitmarker
{
    private static void Prefix(Weapon __instance, GameObject obj)
    {
        if (__instance == null || obj == null)
        {
            return;
        }
        if (!__instance.IsOwner)
        {
            return;
        }
        if (BarrelHitmarkerUtils.IsBarrel(obj))
        {
            HitmarkerManager.PlayBarrel(false);
        }
    }
}

internal static class BarrelHitmarkerUtils
{
    public static bool IsBarrel(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (HasBarrelName(obj.name))
        {
            return true;
        }
        var root = obj.transform != null ? obj.transform.root : null;
        if (root != null && HasBarrelName(root.name))
        {
            return true;
        }
        return false;
    }

    private static bool HasBarrelName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }
        return name.IndexOf("barrel", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Gun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class Gun_GiveDamage_Hitmarker
{
    private static void Prefix(Gun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Shotgun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class Shotgun_GiveDamage_Hitmarker
{
    private static void Prefix(Shotgun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Minigun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class Minigun_GiveDamage_Hitmarker
{
    private static void Prefix(Minigun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(BeamGun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class BeamGun_GiveDamage_Hitmarker
{
    private static void Prefix(BeamGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(ChargeGun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class ChargeGun_GiveDamage_Hitmarker
{
    private static void Prefix(ChargeGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(LargeRaycastGun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class LargeRaycastGun_GiveDamage_Hitmarker
{
    private static void Prefix(LargeRaycastGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(RepulsiveGun), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class RepulsiveGun_GiveDamage_Hitmarker
{
    private static void Prefix(RepulsiveGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(MeleeWeapon), "GiveDamage", new[] { typeof(float), typeof(PlayerHealth), typeof(string) })]
public static class MeleeWeapon_GiveDamage_Hitmarker
{
    private static void Prefix(MeleeWeapon __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        HitmarkerEvents.PlayWeaponDamage(__instance, damageToGive, enemyHealth, name);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Gun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class Gun_KillServer_Hitmarker
{
    private static void Prefix(Gun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Shotgun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class Shotgun_KillServer_Hitmarker
{
    private static void Prefix(Shotgun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Minigun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class Minigun_KillServer_Hitmarker
{
    private static void Prefix(Minigun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(BeamGun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class BeamGun_KillServer_Hitmarker
{
    private static void Prefix(BeamGun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(ChargeGun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class ChargeGun_KillServer_Hitmarker
{
    private static void Prefix(ChargeGun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(LargeRaycastGun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class LargeRaycastGun_KillServer_Hitmarker
{
    private static void Prefix(LargeRaycastGun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(RepulsiveGun), "KillServer", new[] { typeof(PlayerHealth) })]
public static class RepulsiveGun_KillServer_Hitmarker
{
    private static void Prefix(RepulsiveGun __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(MeleeWeapon), "KillServer", new[] { typeof(PlayerHealth) })]
public static class MeleeWeapon_KillServer_Hitmarker
{
    private static void Prefix(MeleeWeapon __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponKill(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Taser), "TaserEnemy", new[] { typeof(PlayerHealth) })]
public static class Taser_TaserEnemy_Hitmarker
{
    private static void Prefix(Taser __instance, PlayerHealth enemyHealth)
    {
        HitmarkerEvents.PlayWeaponContact(__instance, enemyHealth);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(RepulsiveGun), "BumpPlayerServer", new[] { typeof(Vector3), typeof(float), typeof(PlayerHealth) })]
public static class RepulsiveGun_BumpPlayerServer_Hitmarker
{
    private static void Prefix(RepulsiveGun __instance, PlayerHealth ph)
    {
        HitmarkerEvents.PlayWeaponContact(__instance, ph);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(PlayerHealth), "RemoveHealth", new[] { typeof(float) })]
public static class PlayerHealth_RemoveHealth_Hitmarker
{
    private static void Prefix(PlayerHealth __instance, ref float __state)
    {
        __state = __instance != null ? __instance.sync___get_value_health() : 0f;
    }

    private static void Postfix(PlayerHealth __instance, float damage, float __state)
    {
        HitmarkerEvents.PlayRemoveHealthFallback(__instance, __state, damage);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(PhysicsProp), "OnInteract", new[] { typeof(Transform) })]
public static class PhysicsProp_OnInteract_Hitmarker
{
    private static void Prefix(PhysicsProp __instance, Transform player)
    {
        PhysicsPropHitmarkerTracker.Mark(__instance, player);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(PhysicsProp), "OnControllerColliderHit", new[] { typeof(ControllerColliderHit) })]
public static class PhysicsProp_OnControllerColliderHit_Hitmarker
{
    private static readonly FieldInfo damageField = AccessTools.Field(typeof(PhysicsProp), "damage");

    public struct HitState
    {
        public PlayerHealth target;
        public float beforeHealth;
        public float damage;
    }

    private static void Prefix(PhysicsProp __instance, ControllerColliderHit collision, ref HitState __state)
    {
        __state = default(HitState);
        if (!HitmarkerEvents.ShouldWatchPhysicsProp(__instance))
        {
            return;
        }
        if (collision == null || collision.transform == null)
        {
            return;
        }

        PlayerHealth target = collision.transform.GetComponentInParent<PlayerHealth>();
        if (target == null || target.IsOwner)
        {
            return;
        }

        __state.target = target;
        __state.beforeHealth = target.sync___get_value_health();
        __state.damage = GetDamage(__instance);
    }

    private static void Postfix(HitState __state)
    {
        PlayerHealth target = __state.target;
        if (target == null || __state.beforeHealth <= 0f)
        {
            return;
        }

        float afterHealth = target.sync___get_value_health();
        if (afterHealth >= __state.beforeHealth)
        {
            return;
        }

        bool kill = target.sync___get_value_isKilled() || afterHealth <= 0f || __state.beforeHealth - __state.damage <= 0f;
        HitmarkerEvents.PlayPhysicsPropHit(target, kill);
    }

    private static float GetDamage(PhysicsProp prop)
    {
        if (prop == null || damageField == null)
        {
            return 0f;
        }
        object value = damageField.GetValue(prop);
        return value is float ? (float)value : 0f;
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(PredictedProjectile), "HitMarker")]
public static class PredictedProjectile_HitMarker
{
    private static bool Prefix(PredictedProjectile __instance, bool head)
    {
        return HitmarkerPatchUtils.Handle(__instance, head);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(ProximityMine), "HitMarker")]
public static class ProximityMine_HitMarker
{
    private static bool Prefix(ProximityMine __instance, bool head)
    {
        return HitmarkerPatchUtils.Handle(__instance, head);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(ShrapnelBallistic), "HitMarker")]
public static class ShrapnelBallistic_HitMarker
{
    private static bool Prefix(ShrapnelBallistic __instance, bool head)
    {
        return HitmarkerPatchUtils.Handle(__instance, head);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Gun), "ShootServer", new[] { typeof(float), typeof(Vector3), typeof(Vector3) })]
public static class Gun_ShootServer
{
    private static void Postfix(Gun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(BeamGun), "ShootServer", new[] { typeof(float), typeof(Vector3), typeof(Vector3) })]
public static class BeamGun_ShootServer
{
    private static void Postfix(BeamGun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(ChargeGun), "ShootServer", new[] { typeof(float), typeof(Vector3), typeof(Vector3) })]
public static class ChargeGun_ShootServer
{
    private static void Postfix(ChargeGun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Shotgun), "ShootServer", new[] { typeof(float), typeof(Vector3), typeof(Vector3) })]
public static class Shotgun_ShootServer
{
    private static void Postfix(Shotgun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(Minigun), "ShootServer", new[] { typeof(float), typeof(Vector3), typeof(Vector3) })]
public static class Minigun_ShootServer
{
    private static void Postfix(Minigun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(LargeRaycastGun), "ShootServer", new[] { typeof(float), typeof(Vector3), typeof(Vector3), typeof(PlayerHealth) })]
public static class LargeRaycastGun_ShootServer
{
    private static void Postfix(LargeRaycastGun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(LargeRaycastGun), "ShootServerBox", new[] { typeof(float), typeof(Vector3), typeof(Vector3), typeof(PlayerHealth) })]
public static class LargeRaycastGun_ShootServerBox
{
    private static void Postfix(LargeRaycastGun __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}

[HitmarkersPatch]
[HarmonyPatch(typeof(MeleeWeapon), "HitServer", new[] { typeof(PlayerHealth), typeof(Vector3), typeof(Vector3), typeof(string) })]
public static class MeleeWeapon_HitServer
{
    private static void Postfix(MeleeWeapon __instance)
    {
        HitmarkerWeaponPatchUtils.HandleWeaponMarker(__instance);
    }
}
