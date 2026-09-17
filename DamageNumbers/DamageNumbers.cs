using HarmonyLib;
using TMPro;
using UnityEngine;

public static class DamageNumbersSpawner
{
    internal const float DisplayScale = 25f;

    public static void TrySpawn(Weapon weapon, float damage, PlayerHealth target, string hitName)
    {
        if (weapon == null || target == null)
        {
            return;
        }
        if (!weapon.IsOwner)
        {
            return;
        }
        if (damage <= 0f)
        {
            return;
        }
        if (RollingDamageNumbers.TryAdd(weapon, damage, target, hitName))
        {
            return;
        }
        float displayDamage = damage * DisplayScale;
        bool headshot = hitName == "Head_Col" || hitName == "Neck_1_Col";
        Vector3 pos = target.transform.position + Vector3.up * (headshot ? 2.05f : 1.6f);
        GameObject go = new GameObject("DamageNumber");
        go.transform.position = pos;
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = Mathf.CeilToInt(displayDamage).ToString();
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = headshot ? DamageNumbersConfig.HeadshotColor.Value : DamageNumbersConfig.BodyColor.Value;
        text.enableWordWrapping = false;
        text.richText = false;
        DamageNumberStyling.Apply(text);
        go.AddComponent<DamageNumber>();
    }

    public static void TrySpawnProp(Weapon weapon, GameObject obj)
    {
        if (weapon == null || obj == null)
        {
            return;
        }
        if (!weapon.IsOwner)
        {
            return;
        }
        bool isBarrel = DamageNumbersUtils.IsBarrel(obj);
        float damage = weapon.damage * DisplayScale;
        if (!isBarrel && damage <= 0f)
        {
            return;
        }
        Vector3 pos = obj.transform.position + Vector3.up * 1.0f;
        GameObject go = new GameObject("DamageNumber");
        go.transform.position = pos;
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = isBarrel ? ";)" : Mathf.CeilToInt(damage).ToString();
        text.fontSize = 3.0f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = isBarrel ? new Color(0.65f, 0.65f, 0.65f, 1f) : DamageNumbersConfig.BodyColor.Value;
        text.enableWordWrapping = false;
        text.richText = false;
        DamageNumberStyling.Apply(text);
        var number = go.AddComponent<DamageNumber>();
        if (isBarrel)
        {
            number.SuppressBackdrop = true;
            number.OverrideOutlineColor = Color.white;
            number.ApplyOverrideOutline = true;
        }
    }
}

public static class RollingDamageNumbers
{
    private static readonly System.Collections.Generic.Dictionary<int, RollingDamageNumber> activeByTarget = new System.Collections.Generic.Dictionary<int, RollingDamageNumber>(32);

    public static bool TryAdd(Weapon weapon, float damage, PlayerHealth target, string hitName)
    {
        if (!DamageNumbersConfig.EnableRollingDamage.Value)
        {
            return false;
        }
        if (weapon == null || target == null)
        {
            return false;
        }
        if (!weapon.IsOwner)
        {
            return false;
        }
        if (damage <= 0f)
        {
            return false;
        }

        int id = target.GetInstanceID();
        RollingDamageNumber existing;
        if (!activeByTarget.TryGetValue(id, out existing) || existing == null)
        {
            var go = new GameObject("RollingDamageNumber");
            existing = go.AddComponent<RollingDamageNumber>();
            existing.SetTarget(target);
            activeByTarget[id] = existing;
        }

        bool headshot = hitName == "Head_Col" || hitName == "Neck_1_Col";
        existing.AddDamage(damage * DamageNumbersSpawner.DisplayScale, headshot);
        return true;
    }

    public static void RemoveForTarget(int id, RollingDamageNumber instance)
    {
        RollingDamageNumber current;
        if (activeByTarget.TryGetValue(id, out current) && current == instance)
        {
            activeByTarget.Remove(id);
        }
    }
}

public sealed class DamageNumber : MonoBehaviour
{
    private TextMeshPro text;
    private Color baseColor;
    private Vector3 velocity;
    private float lifetime = 1.0f;
    private float elapsed;
    private Camera cam;
    private float scale = 1.0f;
    public bool SuppressBackdrop;
    public bool ApplyOverrideOutline;
    public Color OverrideOutlineColor = Color.white;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
        baseColor = text != null ? text.color : Color.white;
        velocity = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(1.2f, 1.7f), Random.Range(-0.35f, 0.35f));
        if (DamageNumbersConfig.LifetimeSeconds != null)
        {
            lifetime = DamageNumbersConfig.GetLifetimeSeconds();
        }
        if (DamageNumbersConfig.NumberScale != null)
        {
            scale = DamageNumbersConfig.GetNumberScale();
        }
        transform.localScale = Vector3.one * scale;
        if (text != null)
        {
            if (!SuppressBackdrop)
            {
                DamageNumberStyling.AttachBackdrop(text.transform, scale);
            }
            if (ApplyOverrideOutline)
            {
                DamageNumberStyling.OverrideOutlineColor(text, OverrideOutlineColor);
            }
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity.y += 0.6f * Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / lifetime);
        if (text != null)
        {
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(1f, 0f, t));
        }

        cam = GetActiveCamera(cam);
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
            transform.localScale = Vector3.one * scale;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    internal static Camera GetActiveCamera(Camera current)
    {
        if (current != null && current.isActiveAndEnabled)
        {
            return current;
        }
        var main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
        {
            return main;
        }
        Camera best = null;
        float bestDepth = float.MinValue;
        var cams = UnityEngine.Object.FindObjectsOfType<Camera>();
        for (int i = 0; i < cams.Length; i++)
        {
            var c = cams[i];
            if (c == null || !c.isActiveAndEnabled)
            {
                continue;
            }
            if (c.depth >= bestDepth)
            {
                bestDepth = c.depth;
                best = c;
            }
        }
        return best;
    }
}

public sealed class RollingDamageNumber : MonoBehaviour
{
    private TextMeshPro text;
    private PlayerHealth target;
    private int targetId;
    private float totalDamage;
    private float inactivity;
    private float fadeElapsed;
    private bool fading;
    private Camera cam;
    private Vector3 velocity;

    private float windowSeconds;
    private float fadeSeconds;
    private float scale;
    private bool headshot;

    private void Awake()
    {
        text = gameObject.AddComponent<TextMeshPro>();
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.richText = false;
        DamageNumberStyling.Apply(text);

        velocity = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.9f, 1.2f), Random.Range(-0.25f, 0.25f));
        windowSeconds = DamageNumbersConfig.GetRollingWindowSeconds();
        fadeSeconds = DamageNumbersConfig.GetLifetimeSeconds();
        scale = DamageNumbersConfig.GetNumberScale();
        transform.localScale = Vector3.one * scale;
        DamageNumberStyling.AttachBackdrop(text.transform, scale);
    }

    public void SetTarget(PlayerHealth newTarget)
    {
        target = newTarget;
        targetId = newTarget != null ? newTarget.GetInstanceID() : 0;
    }

    public void AddDamage(float damage, bool isHeadshot)
    {
        totalDamage += damage;
        headshot = isHeadshot;
        inactivity = 0f;
        fading = false;
        fadeElapsed = 0f;

        if (text != null)
        {
            text.text = Mathf.CeilToInt(totalDamage).ToString();
            text.color = headshot ? DamageNumbersConfig.HeadshotColor.Value : DamageNumbersConfig.BodyColor.Value;
        }

        UpdatePosition();

        var backdrop = GetComponentInChildren<SpriteRenderer>();
        if (backdrop != null)
        {
            DamageNumberStyling.ResizeBackdrop(backdrop, text);
        }
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        inactivity += Time.deltaTime;

        transform.position += velocity * Time.deltaTime;
        velocity.y += 0.4f * Time.deltaTime;

        if (!fading && inactivity >= windowSeconds)
        {
            fading = true;
            fadeElapsed = 0f;
        }

        if (fading)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeSeconds > 0f ? Mathf.Clamp01(fadeElapsed / fadeSeconds) : 1f;
            if (text != null)
            {
                Color c = text.color;
                text.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t));
            }
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            if (text != null)
            {
                Color c = text.color;
                if (c.a != 1f)
                {
                    text.color = new Color(c.r, c.g, c.b, 1f);
                }
            }
        }

        cam = DamageNumber.GetActiveCamera(cam);
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
            transform.localScale = Vector3.one * scale;
        }
    }

    private void UpdatePosition()
    {
        if (target == null)
        {
            return;
        }
        Vector3 pos = target.transform.position + Vector3.up * (headshot ? 2.05f : 1.6f);
        transform.position = pos;
    }

    private void OnDestroy()
    {
        if (targetId != 0)
        {
            RollingDamageNumbers.RemoveForTarget(targetId, this);
        }
    }
}

public static class DamageNumberStyling
{
    private static Sprite backdropSprite;

    public static void Apply(TextMeshPro text)
    {
        if (text == null)
        {
            return;
        }
        var renderer = text.renderer;
        if (renderer != null)
        {
            renderer.sortingOrder = 1;
        }
        if (DamageNumbersConfig.EnableOutline.Value)
        {
            var mat = text.fontMaterial;
            if (mat != null)
            {
                var newMat = UnityEngine.Object.Instantiate(mat);
                newMat.SetColor("_OutlineColor", DamageNumbersConfig.OutlineColor.Value);
                newMat.SetFloat("_OutlineWidth", DamageNumbersConfig.GetOutlineWidth());
                text.fontMaterial = newMat;
            }
        }
    }

    public static void AttachBackdrop(Transform parent, float scale)
    {
        if (!DamageNumbersConfig.EnableBackdrop.Value || parent == null)
        {
            return;
        }
        var go = new GameObject("DamageNumberBackdrop");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        go.transform.localRotation = Quaternion.identity;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBackdropSprite();
        renderer.color = DamageNumbersConfig.BackdropColor.Value;
        renderer.sortingOrder = 0;
        if (parent.TryGetComponent<Renderer>(out var parentRenderer))
        {
            renderer.sortingLayerID = parentRenderer.sortingLayerID;
        }

        var text = parent.GetComponent<TextMeshPro>();
        if (text != null)
        {
            ResizeBackdrop(renderer, text);
        }
    }

    public static void OverrideOutlineColor(TextMeshPro text, Color outlineColor)
    {
        if (text == null)
        {
            return;
        }
        var mat = text.fontMaterial;
        if (mat == null)
        {
            return;
        }
        var newMat = UnityEngine.Object.Instantiate(mat);
        newMat.SetColor("_OutlineColor", outlineColor);
        newMat.SetFloat("_OutlineWidth", DamageNumbersConfig.GetOutlineWidth());
        text.fontMaterial = newMat;
    }

    public static void ResizeBackdrop(SpriteRenderer renderer, TextMeshPro text)
    {
        if (renderer == null || text == null)
        {
            return;
        }
        text.ForceMeshUpdate();
        var size = text.GetRenderedValues(false);
        float diameter = Mathf.Max(size.x, size.y) * 1.15f;
        renderer.transform.localScale = new Vector3(diameter * DamageNumbersConfig.BackdropScale, diameter * DamageNumbersConfig.BackdropScale, 1f);
    }

    private static Sprite GetBackdropSprite()
    {
        if (backdropSprite != null)
        {
            return backdropSprite;
        }
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = (size - 2) * 0.5f;
        Vector2 center = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist - (r - 2f)) / 2f);
                if (dist > r)
                {
                    alpha = 0f;
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        backdropSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        return backdropSprite;
    }
}

public sealed class ShotgunDamageAggregator : MonoBehaviour
{
    private struct Pending
    {
        public Weapon weapon;
        public PlayerHealth target;
        public float damage;
        public bool headshot;
        public int frame;
    }

    private static ShotgunDamageAggregator instance;
    private static readonly System.Collections.Generic.Dictionary<int, Pending> pendingByTarget = new System.Collections.Generic.Dictionary<int, Pending>(32);
    private static readonly System.Collections.Generic.List<int> pendingKeys = new System.Collections.Generic.List<int>(32);

    public static void Add(Weapon weapon, float damage, PlayerHealth target, string hitName)
    {
        if (weapon == null || target == null)
        {
            return;
        }
        if (!weapon.IsOwner)
        {
            return;
        }
        if (damage <= 0f)
        {
            return;
        }
        EnsureInstance();
        int id = target.GetInstanceID();
        Pending p;
        if (!pendingByTarget.TryGetValue(id, out p) || p.frame != Time.frameCount)
        {
            p = new Pending
            {
                weapon = weapon,
                target = target,
                damage = 0f,
                headshot = false,
                frame = Time.frameCount
            };
        }
        p.damage += damage;
        if (hitName == "Head_Col" || hitName == "Neck_1_Col")
        {
            p.headshot = true;
        }
        pendingByTarget[id] = p;
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }
        var go = new GameObject("ShotgunDamageAggregator");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ShotgunDamageAggregator>();
    }

    private void LateUpdate()
    {
        if (pendingByTarget.Count == 0)
        {
            return;
        }
        pendingKeys.Clear();
        foreach (var kv in pendingByTarget)
        {
            pendingKeys.Add(kv.Key);
        }
        for (int i = 0; i < pendingKeys.Count; i++)
        {
            int key = pendingKeys[i];
            Pending p;
            if (!pendingByTarget.TryGetValue(key, out p))
            {
                continue;
            }
            if (p.weapon != null && p.target != null)
            {
                DamageNumbersSpawner.TrySpawn(p.weapon, p.damage, p.target, p.headshot ? "Head_Col" : "Body");
            }
        }
        pendingByTarget.Clear();
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(Gun), "GiveDamage")]
public static class Gun_DamageNumbers
{
    private static void Prefix(Gun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(Shotgun), "GiveDamage")]
public static class Shotgun_DamageNumbers
{
    private static void Prefix(Shotgun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        ShotgunDamageAggregator.Add(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(Minigun), "GiveDamage")]
public static class Minigun_DamageNumbers
{
    private static void Prefix(Minigun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(BeamGun), "GiveDamage")]
public static class BeamGun_DamageNumbers
{
    private static void Prefix(BeamGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(ChargeGun), "GiveDamage")]
public static class ChargeGun_DamageNumbers
{
    private static void Prefix(ChargeGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(LargeRaycastGun), "GiveDamage")]
public static class LargeRaycastGun_DamageNumbers
{
    private static void Prefix(LargeRaycastGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(RepulsiveGun), "GiveDamage")]
public static class RepulsiveGun_DamageNumbers
{
    private static void Prefix(RepulsiveGun __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(MeleeWeapon), "GiveDamage")]
public static class MeleeWeapon_DamageNumbers
{
    private static void Prefix(MeleeWeapon __instance, float damageToGive, PlayerHealth enemyHealth, string name)
    {
        DamageNumbersSpawner.TrySpawn(__instance, damageToGive, enemyHealth, name);
    }
}

[DamageNumbersPatch]
[HarmonyPatch(typeof(Weapon), "CmdDamageProp")]
public static class Weapon_PropDamageNumbers
{
    private static void Prefix(Weapon __instance, GameObject obj)
    {
        DamageNumbersSpawner.TrySpawnProp(__instance, obj);
    }
}

public static class DamageNumbersUtils
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
        return name.IndexOf("barrel", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static Transform GetLocalPlayerTransform()
    {
        var players = UnityEngine.Object.FindObjectsOfType<FirstPersonController>();
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p != null && p.IsOwner)
            {
                return p.transform;
            }
        }
        return null;
    }
}
