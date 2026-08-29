using UnityEngine;

namespace Blackzone.AI
{
    /// <summary>
    /// Supplies difficulty profiles: designer assets in Resources/AI win,
    /// otherwise built-in ROOKIE / SOLDIER defaults keep the game playable.
    /// </summary>
    public static class AIDifficultyCatalog
    {
        public static AIDifficultyDefinition[] GetDifficulties()
        {
            var assets = Resources.LoadAll<AIDifficultyDefinition>("AI");
            if (assets != null && assets.Length > 0) return assets;
            return new[] { CreateRookie(), CreateSoldier() };
        }

        public static AIDifficultyDefinition Find(string id, AIDifficultyDefinition[] list)
        {
            if (list == null) return null;
            foreach (var d in list)
            {
                if (d != null && d.difficultyId == id) return d;
            }
            return list.Length > 0 ? list[0] : null;
        }

        private static AIDifficultyDefinition CreateRookie()
        {
            var d = ScriptableObject.CreateInstance<AIDifficultyDefinition>();
            d.difficultyId = "rookie";
            d.health = 80f;
            d.moveSpeed = 3.0f;
            d.damagePerShot = 8f;
            d.detectionRange = 34f;
            d.viewAngle = 95f;
            d.reactionTime = 0.7f;
            d.accuracyDegrees = 3.6f;
            d.fireRange = 30f;
            d.burstSize = 3;
            d.burstInterval = 0.13f;
            d.burstCooldown = 1.9f;
            d.repositionInterval = 4.2f;
            d.repositionRadius = 7f;
            d.loseTargetTime = 3.5f;
            d.searchTime = 4f;
            return d;
        }

        private static AIDifficultyDefinition CreateSoldier()
        {
            var d = ScriptableObject.CreateInstance<AIDifficultyDefinition>();
            d.difficultyId = "soldier";
            d.health = 120f;
            d.moveSpeed = 3.6f;
            d.damagePerShot = 11f;
            d.detectionRange = 46f;
            d.viewAngle = 110f;
            d.reactionTime = 0.35f;
            d.accuracyDegrees = 2.2f;
            d.fireRange = 38f;
            d.burstSize = 5;
            d.burstInterval = 0.09f;
            d.burstCooldown = 1.2f;
            d.repositionInterval = 2.8f;
            d.repositionRadius = 9f;
            d.loseTargetTime = 5f;
            d.searchTime = 6f;
            return d;
        }
    }
}
