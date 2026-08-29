using System.Collections;
using Blackzone.Audio;
using Blackzone.Combat;
using Blackzone.Core;
using Blackzone.Utilities;
using Blackzone.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace Blackzone.AI
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Investigate,
        Engage,
        Search,
        Return,
        Dead
    }

    /// <summary>
    /// Prototype soldier AI. Patrols waypoints, detects the player through
    /// line-of-sight checks (cannot see through walls), engages with burst
    /// fire, repositions, loses the target and returns to patrol.
    /// Detection runs on a 0.25s interval coroutine to keep CPU low on mobile.
    /// </summary>
    public sealed class EnemySoldier : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Health health;
        private AIDifficultyDefinition diff;
        private Transform player;

        private Transform headBone;
        private Transform muzzle;
        private CapsuleCollider bodyCollider;
        private SphereCollider headCollider;
        private Material bodyMaterial;
        private Color originalColor;

        private EnemyState state = EnemyState.Idle;
        private Vector3[] waypoints;
        private int waypointIndex;
        private float waypointWait;

        private Vector3 lastKnownPlayerPos;
        private float reactionTimer;
        private bool reacting;

        private int burstShotsLeft;
        private float shotTimer;
        private float burstCooldownTimer;
        private float repositionTimer;
        private float loseSightTimer;
        private float searchTimer;
        private float searchSpin;

        public EnemyState State => state;
        public bool IsDead => health != null && health.IsDead;

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= OnDied;
                health.Damaged -= OnDamaged;
            }
        }

        public void Init(AIDifficultyDefinition difficulty, Transform playerRoot,
            Vector3[] patrolPoints, Transform parent, int slot)
        {
            diff = difficulty;
            player = playerRoot;
            waypoints = patrolPoints;

            transform.SetParent(parent, false);
            gameObject.layer = GameConstants.LayerEnemy;

            // --- Visuals (placeholder) ---
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.Destroy(body.GetComponent<Collider>()); // decorative only
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.72f, 0.95f, 0.72f);
            var bodyShader = Shader.Find("Universal Render Pipeline/Lit");
            if (bodyShader == null) bodyShader = Shader.Find("Standard");
            var bodyMat = new Material(bodyShader);
            bodyMat.color = slot % 2 == 0
                ? new Color(0.55f, 0.50f, 0.36f)   // rookie: khaki
                : new Color(0.34f, 0.39f, 0.30f);  // soldier: olive
            bodyMat.SetFloat("_Smoothness", 0.3f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            bodyMaterial = bodyMat;
            originalColor = bodyMat.color;

            // Head is its OWN object: mesh + collider + HitRegion on the same GO
            // so hit detection can distinguish head shots from body shots.
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.layer = GameConstants.LayerEnemy;
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            var headRegion = head.AddComponent<HitRegion>();
            headRegion.Configure(true);
            headCollider = head.GetComponent<SphereCollider>();
            headCollider.radius = 0.5f; // scales with the 0.32 mesh -> ~0.16 world

            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "Gun";
            Object.Destroy(gun.GetComponent<Collider>()); // decorative only
            gun.transform.SetParent(transform, false);
            gun.transform.localPosition = new Vector3(0f, 1.42f, 0.28f);
            gun.transform.localScale = new Vector3(0.07f, 0.09f, 0.5f);
            gun.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

            // --- Colliders (body hitbox on the root) ---
            bodyCollider = gameObject.AddComponent<CapsuleCollider>();
            bodyCollider.radius = 0.36f;
            bodyCollider.height = 1.8f;
            bodyCollider.center = new Vector3(0f, 0.9f, 0f);

            // --- NavMeshAgent ---
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.speed = diff.moveSpeed;
            agent.angularSpeed = 420f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.8f;
            agent.autoBraking = true;
            agent.updateRotation = false; // we drive facing manually

            // --- Combat ---
            health = gameObject.AddComponent<Health>();
            health.Initialize(diff.health);
            health.Died += OnDied;
            health.Damaged += OnDamaged;

            headBone = new GameObject("AimBone").transform;
            headBone.SetParent(transform, false);
            headBone.localPosition = new Vector3(0f, 1.6f, 0.2f);

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(headBone, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.35f);

            state = EnemyState.Idle;
        }

        private void OnEnable()
        {
            StartCoroutine(DetectionLoop());
        }

        public void ResetForEncounter(Vector3 spawnPosition)
        {
            health.Revive();
            agent.enabled = true;
            agent.Warp(spawnPosition);
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            state = EnemyState.Patrol;
            waypointIndex = 0;
            waypointWait = Random.Range(diff.patrolWaitMin, diff.patrolWaitMax);
            reacting = false;
            burstShotsLeft = 0;
            burstCooldownTimer = 0f;
            repositionTimer = diff.repositionInterval;
            loseSightTimer = 0f;
            searchTimer = 0f;
            bodyCollider.enabled = true;
            headCollider.enabled = true;
            if (bodyMaterial != null) bodyMaterial.color = originalColor;
        }

        // ---------------------------------------------------------------
        // Detection (throttled for mobile CPU)
        // ---------------------------------------------------------------
        private IEnumerator DetectionLoop()
        {
            var wait = new WaitForSeconds(0.25f);
            while (true)
            {
                yield return wait;
                if (state == EnemyState.Dead || IsDead) continue;
                if (player == null) continue;
                if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                    continue;

                float dist;
                if (CanSeePlayer(out dist))
                {
                    lastKnownPlayerPos = player.position;
                    if (state == EnemyState.Patrol || state == EnemyState.Return || state == EnemyState.Idle)
                        BeginEngage();
                }
                else if (state == EnemyState.Engage)
                {
                    loseSightTimer += 0.25f;
                    if (loseSightTimer >= diff.loseTargetTime)
                    {
                        loseSightTimer = 0f;
                        EnterSearch();
                    }
                }
            }
        }

        private bool CanSeePlayer(out float distance)
        {
            distance = float.MaxValue;
            if (player == null) return false;
            var playerHealth = player.GetComponent<Health>();
            if (playerHealth != null && playerHealth.IsDead) return false;

            Vector3 toPlayer = player.position - headBone.position;
            distance = toPlayer.magnitude;
            if (distance > diff.detectionRange) return false;

            if (Vector3.Angle(headBone.forward, toPlayer) > diff.viewAngle * 0.5f) return false;

            return !Physics.Raycast(headBone.position, toPlayer, distance,
                GameConstants.EnemyVisionMask, QueryTriggerInteraction.Ignore);
        }

        // ---------------------------------------------------------------
        // State transitions
        // ---------------------------------------------------------------
        private void BeginEngage()
        {
            state = EnemyState.Engage;
            reacting = true;
            reactionTimer = diff.reactionTime;
            burstCooldownTimer = 0.4f;
            repositionTimer = diff.repositionInterval;
            loseSightTimer = 0f;
        }

        private void EnterSearch()
        {
            state = EnemyState.Search;
            searchTimer = diff.searchTime;
            searchSpin = 0f;
            agent.isStopped = true;
            agent.ResetPath();
        }

        private void OnDamaged(Health h, float amount)
        {
            if (state == EnemyState.Dead || IsDead) return;
            lastKnownPlayerPos = player != null ? player.position : transform.position;
            if (state != EnemyState.Engage)
            {
                // Shot: snap to engage faster than normal detection.
                state = EnemyState.Engage;
                reacting = true;
                reactionTimer = diff.reactionTime * 0.5f;
                burstCooldownTimer = 0.25f;
            }
        }

        private void OnDied(Health h)
        {
            state = EnemyState.Dead;
            agent.enabled = false;
            bodyCollider.enabled = false;
            headCollider.enabled = false;
            if (bodyMaterial != null) bodyMaterial.color = new Color(0.16f, 0.14f, 0.12f);
            // Smooth fall-over: tilt sideways in random direction
            float fallDir = Random.value > 0.5f ? -90f : 90f;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, fallDir);
            AudioManager.Instance.Play(AudioId.EnemyDeath, 1f);
            GameEvents.EmitEnemyKilled();
            // Spawn dust puff at death position
            if (WeaponFx.HasInit())
                WeaponFx.SpawnImpact(transform.position + Vector3.up * 0.3f, Vector3.up);
        }

        // ---------------------------------------------------------------
        // Per-frame behavior
        // ---------------------------------------------------------------
        private void Update()
        {
            if (state == EnemyState.Dead || IsDead) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float dt = Time.deltaTime;
            switch (state)
            {
                case EnemyState.Patrol: UpdatePatrol(dt); break;
                case EnemyState.Investigate: UpdateInvestigate(dt); break;
                case EnemyState.Engage: UpdateEngage(dt); break;
                case EnemyState.Search: UpdateSearch(dt); break;
                case EnemyState.Return: UpdateReturn(dt); break;
            }
        }

        private void UpdatePatrol(float dt)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                state = EnemyState.Idle;
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;
            if (!agent.hasPath || agent.pathPending)
            {
                agent.SetDestination(waypoints[waypointIndex]);
                FaceMovementDirection(dt);
                return;
            }

            if (agent.remainingDistance < 1.4f)
            {
                waypointWait -= dt;
                if (waypointWait <= 0f)
                {
                    waypointIndex = (waypointIndex + 1) % waypoints.Length;
                    waypointWait = Random.Range(diff.patrolWaitMin, diff.patrolWaitMax);
                    agent.SetDestination(waypoints[waypointIndex]);
                }
            }
        }

        private void UpdateInvestigate(float dt)
        {
            agent.isStopped = false;
            agent.SetDestination(lastKnownPlayerPos);
            FaceMovementDirection(dt);
            if (agent.remainingDistance < 1.5f)
            {
                searchTimer = diff.searchTime;
                searchSpin = 0f;
                agent.isStopped = true;
                agent.ResetPath();
                state = EnemyState.Search;
            }
        }

        private void UpdateEngage(float dt)
        {
            if (player == null) return;

            Vector3 flatDir = player.position - transform.position;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(flatDir.normalized), 8f * dt);

            if (reacting)
            {
                reactionTimer -= dt;
                if (reactionTimer <= 0f) reacting = false;
                return;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            // Reposition periodically (unless close enough to hold).
            repositionTimer -= dt;
            if (repositionTimer <= 0f && dist > 6f)
            {
                repositionTimer = diff.repositionInterval;
                Vector3 offset = Random.insideUnitSphere * diff.repositionRadius;
                offset.y = 0f;
                Vector3 target = player.position + offset;
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(target, out navHit, diff.repositionRadius * 0.6f, NavMesh.AllAreas))
                    agent.SetDestination(navHit.position);
            }

            // Burst firing.
            if (dist < diff.fireRange)
            {
                burstCooldownTimer -= dt;
                if (burstShotsLeft <= 0 && burstCooldownTimer <= 0f)
                {
                    burstShotsLeft = diff.burstSize;
                    shotTimer = 0f;
                }
                else if (burstShotsLeft > 0)
                {
                    shotTimer -= dt;
                    if (shotTimer <= 0f)
                    {
                        FireShot();
                        burstShotsLeft--;
                        shotTimer = diff.burstInterval;
                        if (burstShotsLeft == 0)
                            burstCooldownTimer = diff.burstCooldown;
                    }
                }
            }
        }

        private void UpdateSearch(float dt)
        {
            searchTimer -= dt;
            searchSpin += 200f * dt;
            transform.rotation = Quaternion.Euler(0f, searchSpin, 0f);

            if (searchTimer <= 0f)
            {
                state = EnemyState.Return;
                agent.isStopped = false;
                agent.SetDestination(NearestWaypoint());
            }
        }

        private void UpdateReturn(float dt)
        {
            agent.isStopped = false;
            FaceMovementDirection(dt);
            if (agent.remainingDistance < 1.5f)
            {
                waypointIndex = 0;
                waypointWait = Random.Range(diff.patrolWaitMin, diff.patrolWaitMax);
                state = EnemyState.Patrol;
            }
        }

        private Vector3 NearestWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return transform.position;
            Vector3 best = waypoints[0];
            float bestDist = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                float d = Vector3.SqrMagnitude(waypoints[i] - transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = waypoints[i];
                }
            }
            return best;
        }

        private void FaceMovementDirection(float dt)
        {
            Vector3 vel = agent.velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude > 0.5f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(vel.normalized), 8f * dt);
        }

        private void FireShot()
        {
            if (player == null) return;

            Vector3 target = player.position + Vector3.up * 1.1f;
            Vector3 dir = target - muzzle.position;
            float dist = dir.magnitude;
            if (dist <= 0.01f) return;
            dir /= dist;

            float spread = diff.accuracyDegrees;
            dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * dir;

            if (Ballistics.FireEnemyRay(muzzle.position, dir, diff.fireRange, diff.damagePerShot,
                    out RaycastHit hit, out bool killedPlayer, out bool headshot))
            {
                if (hit.collider != null)
                    WeaponFx.SpawnImpact(hit.point, hit.normal);
                if (headshot && hit.collider != null &&
                    hit.collider.GetComponentInParent<Health>() != null)
                    GameEvents.EmitHitConfirmed();
            }

            WeaponFx.SpawnMuzzleFlash(muzzle.position, dir);
            AudioManager.Instance.Play(AudioId.EnemyFire, 1f);
        }
    }
}
