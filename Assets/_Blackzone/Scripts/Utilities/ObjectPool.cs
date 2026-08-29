using System.Collections.Generic;
using UnityEngine;

namespace Blackzone.Utilities
{
    /// <summary>
    /// Minimal pooled FX spawner for tracers, muzzle flashes and impact sparks.
    /// Objects are keyed by their prototype prefab name; a soft cap prevents
    /// unbounded growth on long sessions.
    /// </summary>
    public sealed class ObjectPool : MonoBehaviour
    {
        private static ObjectPool instance;
        public static ObjectPool Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[ObjectPool]");
                    instance = go.AddComponent<ObjectPool>();
                }
                return instance;
            }
        }

        private const int MaxPerPool = 48;

        private readonly Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>();

        /// <summary>Returns a pooled clone, or null if the pool is saturated.</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null) return null;

            Stack<GameObject> pool;
            if (!pools.TryGetValue(prefab.name, out pool))
            {
                pool = new Stack<GameObject>();
                pools[prefab.name] = pool;
            }

            if (pool.Count > MaxPerPool) return null;

            GameObject item = pool.Count > 0 ? pool.Pop() : null;
            if (item == null)
            {
                item = Instantiate(prefab);
                item.name = prefab.name;
            }

            item.transform.SetParent(transform, false);
            item.transform.position = position;
            item.transform.rotation = rotation;
            item.SetActive(true);

            var fx = item.GetComponent<PooledFx>();
            if (fx == null) fx = item.AddComponent<PooledFx>();
            fx.Init(lifetime, pool);
            return item;
        }
    }

    /// <summary>Despawn helper attached to every pooled object.</summary>
    public sealed class PooledFx : MonoBehaviour
    {
        private Stack<GameObject> pool;
        private float timer;
        private float lifetime;

        public void Init(float life, Stack<GameObject> returnPool)
        {
            pool = returnPool;
            lifetime = life;
            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                if (pool != null)
                {
                    gameObject.SetActive(false);
                    pool.Push(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
