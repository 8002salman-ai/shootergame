using System.Collections.Generic;
using Blackzone.Core;
using Blackzone.World;
using UnityEngine;

namespace Blackzone.AI
{
    /// <summary>
    /// Builds and owns the encounter's enemy roster. Mixes ROOKIE and SOLDIER
    /// profiles across the map's spawn points; resets them on encounter restart.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        private readonly List<EnemySoldier> enemies = new List<EnemySoldier>();
        private Vector3[] spawnPoints;
        private int total;

        public int Total => total;

        public static EnemySpawner Build(Transform parent, MapLayout map, Transform playerRoot)
        {
            var go = new GameObject("EnemySpawner");
            go.transform.SetParent(parent, false);
            var spawner = go.AddComponent<EnemySpawner>();

            var difficulties = AIDifficultyCatalog.GetDifficulties();
            var rookie = AIDifficultyCatalog.Find("rookie", difficulties) ?? difficulties[0];
            var soldier = AIDifficultyCatalog.Find("soldier", difficulties) ?? difficulties[0];

            spawner.spawnPoints = map.EnemySpawns;
            for (int i = 0; i < map.EnemySpawns.Length; i++)
            {
                var soldierGo = new GameObject($"Enemy_{i:00}");
                var enemy = soldierGo.AddComponent<EnemySoldier>();
                var difficulty = i % 2 == 0 ? rookie : soldier;
                enemy.Init(difficulty, playerRoot, map.Waypoints, go.transform, i);
                spawner.enemies.Add(enemy);
            }
            spawner.total = map.EnemySpawns.Length;
            spawner.ResetAll();
            return spawner;
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled()
        {
            GameEvents.EmitEnemiesRemaining(AliveCount(), total);
        }

        public int AliveCount()
        {
            int alive = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && !enemies[i].IsDead) alive++;
            }
            return alive;
        }

        public void ResetAll()
        {
            if (spawnPoints == null) return;
            for (int i = 0; i < enemies.Count && i < spawnPoints.Length; i++)
            {
                if (enemies[i] != null)
                    enemies[i].ResetForEncounter(spawnPoints[i]);
            }
            GameEvents.EmitEnemiesRemaining(AliveCount(), total);
        }
    }
}
