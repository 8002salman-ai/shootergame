namespace Blackzone.Combat
{
    public enum DamageSource { Player, Enemy, Environment }

    /// <summary>Immutable damage packet passed through health/armor.</summary>
    public readonly struct DamageInfo
    {
        public readonly float Damage;
        public readonly DamageSource Source;
        public readonly bool IsHeadshot;
        public readonly string WeaponName;

        public DamageInfo(float damage, DamageSource source, bool isHeadshot = false, string weaponName = "")
        {
            Damage = damage;
            Source = source;
            IsHeadshot = isHeadshot;
            WeaponName = weaponName;
        }
    }
}
