using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class DamageNumbersPatchAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class HitmarkersPatchAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class HitSoundsPatchAttribute : Attribute
{
}
