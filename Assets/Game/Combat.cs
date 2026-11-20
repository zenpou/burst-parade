using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parade
{
    public enum Phase
    {
        Title,
        Run,
        Clear,
        Defeat
    }

    public enum Kind
    {
        Scout,
        Gunner,
        Heavy,
        Diver,
        Mine,
        Turret,
        Winder,
        Splitter,
        Shield,
        Lancer,
        Boss,
        Shot,
        Bullet
    }

    /// <summary>
    /// A lightweight renderable object used by the game loop and the presentation layer.
    /// Timer and phase are intentionally public so a renderer can add movement or hit-flash
    /// accents without keeping a second copy of combat state.
    /// </summary>
    public sealed class Unit
    {
        public Kind kind;
        public Vector2 pos;
        public Vector2 vel;
        public int hp;
        public int maxHp;
        public float age;
        public int id;

        public float timer;
        public float phase;
        public float radius;
        /// <summary>0..1 attack-warning amount. Presentation can draw this before a volley.</summary>
        public float telegraph;
        /// <summary>Chapter/pattern variant. Enemy art and behavior can branch on this.</summary>
        public int variant;
        /// <summary>Projectile damage. Enemy projectiles remain one-hit by default.</summary>
        public int damage = 1;
        /// <summary>0..1 defensive guard openness. A closed shield is below the opening threshold.</summary>
        public float guard;
        public bool guardOpen;
        public bool committed;
        public float actionTimer;
        public int pattern;

        public Unit(Kind unitKind, Vector2 position, Vector2 velocity, int health, int unitId)
        {
            kind = unitKind;
            pos = position;
            vel = velocity;
            hp = health;
            maxHp = health;
            id = unitId;
            radius = DefaultRadius(unitKind);
        }

        private static float DefaultRadius(Kind unitKind)
        {
            switch (unitKind)
            {
                case Kind.Boss:
                    return 1.15f;
                case Kind.Heavy:
                    return 0.68f;
                case Kind.Shield:
                    return 0.78f;
                case Kind.Diver:
                    return 0.52f;
                case Kind.Turret:
                    return 0.56f;
                case Kind.Winder:
                    return 0.54f;
                case Kind.Splitter:
                    return 0.62f;
                case Kind.Lancer:
                    return 0.48f;
                case Kind.Mine:
                    return 0.38f;
                case Kind.Gunner:
                    return 0.5f;
                case Kind.Scout:
                    return 0.42f;
                case Kind.Shot:
                    return 0.16f;
                default:
                    return 0.19f;
            }
        }
    }

    /// <summary>
    /// Deterministic, presentation-independent combat simulation for BURST PARADE.
    /// The host MonoBehaviour supplies a movement vector and a one-frame burst request,
    /// then reads player, units, score, and phase for rendering.
    /// </summary>
    public sealed class Combat
    {
        private const int ChapterCount = 5;
        private const int PilotCount = 3;

        public Phase phase = Phase.Title;
        public List<Unit> units = new List<Unit>(96);
        public Vector2 player = new Vector2(-6f, 0f);

        /// <summary>0..4, selected story stage. Kept public for the campaign UI.</summary>
        public int chapter;
        /// <summary>0 Akari, 1 Mio, 2 Sena.</summary>
        public int pilot;

        public float time;
        public float charge;
        public float invuln;
        public float comboTime;
        public float bossRatio;

        public int hull;
        public int score;
        public int combo;
        public int maxCombo;
        public int kills;
        public int bursts;
        public bool bossSpawned;

        public Action<string, Vector2, int> signal;

        internal const float PlayerBulletRadius = .27f;
        private const float PlayerMinX = -8f;
        private const float PlayerMaxX = 5.7f;
        private const float PlayerMinY = -3.7f;
        private const float PlayerMaxY = 3.7f;
        private const float WorldMinX = -9.4f;
        private const float WorldMaxX = 9.7f;
        private const float WorldMinY = -4.6f;
        private const float WorldMaxY = 4.6f;

        private const float ComboWindow = 1.75f;
        private const float PlayerSpeed = 7.8f;

        private float shotTimer;
        private float spawnTimer;
        private Vector2 bossBreak;
        private int waveIndex;
        private int nextId;
        private uint randomState = 0xA53C9E71u;

        private float BossStartTimeForChapter
        {
            get { return chapter == 4 ? 64f : chapter == 3 ? 61f : chapter == 2 ? 60f : 58f; }
        }

        public void StartRun()
        {
            StartRun(0, 0);
        }

        public void StartRun(int selectedChapter, int selectedPilot)
        {
            chapter = Mathf.Clamp(selectedChapter, 0, ChapterCount - 1);
            pilot = Mathf.Clamp(selectedPilot, 0, PilotCount - 1);
            phase = Phase.Run;
            units.Clear();
            player = new Vector2(-6f, 0f);
            time = 0f;
            charge = 0.2f;
            invuln = 0f;
            comboTime = 0f;
            bossRatio = 0f;
            hull = 3;
            score = 0;
            combo = 0;
            maxCombo = 0;
            kills = 0;
            bursts = 0;
            bossSpawned = false;
            shotTimer = 0.05f;
            spawnTimer = 0.25f;
            waveIndex = 0;
            nextId = 1;
            randomState = 0xA53C9E71u + (uint)(chapter * 977 + pilot * 53);
        }

        public void Step(float dt, Vector2 move, bool burst)
        {
            if (phase != Phase.Run)
                return;

            // Keep a stalled frame from teleporting through a collision, while still
            // allowing a normal frame-rate range and simple fixed-step hosts.
            dt = Mathf.Clamp(dt, 0.0001f, 0.1f);

            time += dt;
            invuln = Mathf.Max(0f, invuln - dt);
            comboTime -= dt;
            if (comboTime <= 0f)
                combo = 0;

            player += Vector2.ClampMagnitude(move, 1f) * (PlayerSpeed * dt);
            player.x = Mathf.Clamp(player.x, PlayerMinX, PlayerMaxX);
            player.y = Mathf.Clamp(player.y, PlayerMinY, PlayerMaxY);

            if (!bossSpawned && time >= BossStartTimeForChapter)
                SpawnBoss();

            if (!bossSpawned)
                SpawnRegularWave(dt);

            if (burst && charge >= 0.999f)
                TriggerBurst();

            UpdateUnits(dt);
            FirePlayerShot(dt);
            ResolvePlayerHits();
            if (phase != Phase.Run)
                return;
            ResolveShotHits();
            ResolveEnemyDeaths();

            if (bossSpawned)
                UpdateBossRatio();

            if (phase == Phase.Run && bossSpawned && bossRatio <= 0f)
            {
                phase = Phase.Clear;
                Emit("clear", bossBreak, score);
            }
        }

        private void SpawnRegularWave(float dt)
        {
            spawnTimer -= dt;
            int spawned = 0;
            while (spawnTimer <= 0f && time < BossStartTimeForChapter && spawned < 2)
            {
                SpawnWave();
                spawned++;
                float pace = Mathf.Lerp(0.96f, 0.43f, Mathf.Clamp01(time / BossStartTimeForChapter));
                spawnTimer += pace;
            }
        }

        private void SpawnWave()
        {
            // A deterministic sequence keeps each chapter readable and learnable.
            // The small lane gaps are intentional, giving the player a safe reaction beat.
            int slot = waveIndex++;
            float lane = Lane(slot + chapter * 3);
            int beat = slot % 12;
            if (time < 5f)
            {
                AddEnemy(Kind.Scout, new Vector2(9.1f, lane), new Vector2(-2.5f, 0), 1, 0);
                AddEnemy(Kind.Scout, new Vector2(9.85f, lane + 0.18f), new Vector2(-2.5f, 0), 1, 0);
                return;
            }

            switch (chapter)
            {
                case 0:
                    SpawnHarborWave(beat, lane);
                    break;
                case 1:
                    SpawnAbyssWave(beat, lane);
                    break;
                case 2:
                    SpawnGardenWave(beat, lane);
                    break;
                case 3:
                    SpawnOrbitWave(beat, lane);
                    break;
                default:
                    SpawnSignalWave(beat, lane);
                    break;
            }
        }

        private float Lane(int index)
        {
            // Eight lanes with a repeatable zig-zag. Keep the center lane open often.
            int lane = ((index * 5 + index / 3) % 9) - 4;
            return lane * 0.72f;
        }

        private void SpawnHarborWave(int beat, float lane)
        {
            switch (beat)
            {
                case 0: case 1:
                    AddEnemy(Kind.Scout, new Vector2(9.25f, lane), new Vector2(-2.7f, 0), 1, 0);
                    AddEnemy(Kind.Scout, new Vector2(9.70f, lane + 0.85f), new Vector2(-2.45f, 0), 1, 1.4f); break;
                case 2: case 3:
                    AddEnemy(Kind.Gunner, new Vector2(9.35f, lane), new Vector2(-1.42f, 0), 3, 0); break;
                case 4:
                    AddEnemy(Kind.Diver, new Vector2(9.45f, lane), new Vector2(-1.75f, 0), 4, 0.2f); break;
                case 5:
                    AddEnemy(Kind.Mine, new Vector2(9.4f, lane), new Vector2(-1.0f, 0), 2, 0); break;
                case 6: case 7:
                    AddEnemy(Kind.Heavy, new Vector2(9.40f, lane), new Vector2(-.9f, 0), 8, .3f); break;
                case 8:
                    AddEnemy(Kind.Turret, new Vector2(9.5f, lane), new Vector2(-.75f, 0), 5, .5f); break;
                default:
                    AddEnemy(Kind.Scout, new Vector2(9.3f, lane), new Vector2(-2.4f, 0), 1, 0);
                    AddEnemy(Kind.Mine, new Vector2(9.8f, lane - .85f), new Vector2(-1.2f, 0), 2, 0); break;
            }
        }

        private void SpawnAbyssWave(int beat, float lane)
        {
            switch (beat)
            {
                case 0: case 1:
                    AddEnemy(Kind.Diver, new Vector2(9.4f, lane), new Vector2(-1.9f, 0), 4, 0); break;
                case 2:
                    AddEnemy(Kind.Winder, new Vector2(9.5f, lane), new Vector2(-1.65f, 0), 5, .9f); break;
                case 3: case 4:
                    AddEnemy(Kind.Mine, new Vector2(9.35f, lane), new Vector2(-1.1f, 0), 2, 0);
                    AddEnemy(Kind.Diver, new Vector2(9.8f, -lane), new Vector2(-1.55f, 0), 4, 1.8f); break;
                case 5:
                    AddEnemy(Kind.Turret, new Vector2(9.55f, lane), new Vector2(-.62f, 0), 5, .2f); break;
                case 6: case 7:
                    AddEnemy(Kind.Gunner, new Vector2(9.4f, lane), new Vector2(-1.35f, 0), 3, 0);
                    AddEnemy(Kind.Mine, new Vector2(9.9f, lane + .7f), new Vector2(-1.05f, 0), 2, 0); break;
                case 8:
                    AddEnemy(Kind.Winder, new Vector2(9.4f, lane), new Vector2(-1.45f, 0), 5, 2.2f); break;
                default:
                    AddEnemy(Kind.Heavy, new Vector2(9.4f, lane), new Vector2(-.86f, 0), 8, 0); break;
            }
        }

        private void SpawnGardenWave(int beat, float lane)
        {
            switch (beat)
            {
                case 0: case 1:
                    AddEnemy(Kind.Splitter, new Vector2(9.4f, lane), new Vector2(-1.5f, 0), 5, 0); break;
                case 2: case 3:
                    AddEnemy(Kind.Shield, new Vector2(9.45f, lane), new Vector2(-.95f, 0), 9, .2f); break;
                case 4:
                    AddEnemy(Kind.Winder, new Vector2(9.45f, lane), new Vector2(-1.45f, 0), 5, 1.4f); break;
                case 5: case 6:
                    AddEnemy(Kind.Diver, new Vector2(9.3f, lane), new Vector2(-1.8f, 0), 4, 0);
                    AddEnemy(Kind.Splitter, new Vector2(9.85f, lane - .9f), new Vector2(-1.4f, 0), 5, 0); break;
                case 7:
                    AddEnemy(Kind.Turret, new Vector2(9.5f, lane), new Vector2(-.62f, 0), 5, 0); break;
                case 8: case 9:
                    AddEnemy(Kind.Shield, new Vector2(9.4f, lane), new Vector2(-.92f, 0), 9, .8f); break;
                default:
                    AddEnemy(Kind.Scout, new Vector2(9.3f, lane), new Vector2(-2.6f, 0), 1, 0); break;
            }
        }

        private void SpawnOrbitWave(int beat, float lane)
        {
            switch (beat)
            {
                case 0:
                    AddEnemy(Kind.Lancer, new Vector2(9.45f, lane), new Vector2(-1.8f, 0), 4, 0); break;
                case 1: case 2:
                    AddEnemy(Kind.Turret, new Vector2(9.45f, lane), new Vector2(-.55f, 0), 5, .2f);
                    AddEnemy(Kind.Mine, new Vector2(9.9f, lane + .85f), new Vector2(-1.05f, 0), 2, 0); break;
                case 3: case 4:
                    AddEnemy(Kind.Shield, new Vector2(9.4f, lane), new Vector2(-.9f, 0), 9, 1.1f); break;
                case 5:
                    AddEnemy(Kind.Lancer, new Vector2(9.4f, lane), new Vector2(-1.65f, 0), 4, 1.9f);
                    AddEnemy(Kind.Scout, new Vector2(9.9f, -lane), new Vector2(-2.8f, 0), 1, 0); break;
                case 6: case 7:
                    AddEnemy(Kind.Winder, new Vector2(9.4f, lane), new Vector2(-1.5f, 0), 5, .4f); break;
                case 8:
                    AddEnemy(Kind.Heavy, new Vector2(9.4f, lane), new Vector2(-.86f, 0), 8, 0); break;
                default:
                    AddEnemy(Kind.Lancer, new Vector2(9.4f, lane), new Vector2(-1.5f, 0), 4, .6f); break;
            }
        }

        private void SpawnSignalWave(int beat, float lane)
        {
            switch (beat)
            {
                case 0:
                    AddEnemy(Kind.Lancer, new Vector2(9.4f, lane), new Vector2(-1.7f, 0), 4, 0);
                    AddEnemy(Kind.Winder, new Vector2(9.85f, -lane), new Vector2(-1.45f, 0), 5, .3f); break;
                case 1: case 2:
                    AddEnemy(Kind.Shield, new Vector2(9.4f, lane), new Vector2(-.9f, 0), 9, .7f);
                    AddEnemy(Kind.Turret, new Vector2(9.85f, -lane), new Vector2(-.65f, 0), 5, .1f); break;
                case 3:
                    AddEnemy(Kind.Splitter, new Vector2(9.4f, lane), new Vector2(-1.4f, 0), 5, 0); break;
                case 4: case 5:
                    AddEnemy(Kind.Diver, new Vector2(9.3f, lane), new Vector2(-1.75f, 0), 4, .8f);
                    AddEnemy(Kind.Mine, new Vector2(9.8f, -lane), new Vector2(-1.0f, 0), 2, 0); break;
                case 6:
                    AddEnemy(Kind.Lancer, new Vector2(9.4f, lane), new Vector2(-1.7f, 0), 4, 1.7f); break;
                case 7: case 8:
                    AddEnemy(Kind.Heavy, new Vector2(9.4f, lane), new Vector2(-.82f, 0), 8, .4f); break;
                case 9:
                    AddEnemy(Kind.Shield, new Vector2(9.4f, lane), new Vector2(-.9f, 0), 9, 1.5f); break;
                default:
                    AddEnemy(Kind.Winder, new Vector2(9.4f, lane), new Vector2(-1.5f, 0), 5, 2.3f); break;
            }
        }

        private void SpawnBoss()
        {
            bossSpawned = true;
            int health = 280 + chapter * 22;
            Unit boss = AddEnemy(Kind.Boss, new Vector2(9.1f, 0f), new Vector2(-0.75f, 0f), health, 0f);
            boss.variant = chapter;
            boss.timer = 1.30f;
            bossRatio = 1f;
            Emit("boss", boss.pos, boss.maxHp);
        }

        private Unit AddEnemy(Kind kind, Vector2 position, Vector2 velocity, int health, float phaseOffset)
        {
            Unit enemy = new Unit(kind, position, velocity, health, nextId++);
            enemy.phase = phaseOffset;
            enemy.variant = chapter;
            enemy.timer = kind == Kind.Lancer ? 1.05f :
                kind == Kind.Diver ? .65f : kind == Kind.Mine ? 1.05f :
                kind == Kind.Turret ? .85f : kind == Kind.Winder ? .72f :
                kind == Kind.Splitter ? .95f : kind == Kind.Shield ? 1.20f : 0f;
            units.Add(enemy);
            return enemy;
        }

        private Unit AddProjectile(Kind kind, Vector2 position, Vector2 velocity, float radius)
        {
            Unit projectile = new Unit(kind, position, velocity, 1, nextId++);
            projectile.radius = radius;
            units.Add(projectile);
            return projectile;
        }

        private void UpdateUnits(float dt)
        {
            for (int index = units.Count - 1; index >= 0; index--)
            {
                Unit unit = units[index];
                unit.age += dt;
                unit.timer -= dt;

                if (unit.kind == Kind.Shot || unit.kind == Kind.Bullet)
                {
                    unit.pos += unit.vel * dt;
                    if (unit.pos.x < WorldMinX || unit.pos.x > WorldMaxX || unit.pos.y < WorldMinY || unit.pos.y > WorldMaxY)
                        units.RemoveAt(index);
                    continue;
                }

                if (unit.kind == Kind.Boss)
                {
                    UpdateBoss(unit, dt);
                    continue;
                }

                PrimeTelegraph(unit);
                unit.pos += unit.vel * dt;
                if (unit.kind == Kind.Scout)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 4.2f + unit.phase) * 1.15f * dt;
                }
                else if (unit.kind == Kind.Gunner)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 2.1f + unit.phase) * 0.72f * dt;
                    if (unit.timer <= 0f)
                    {
                        AimBullet(unit.pos, 3.2f, 0.18f);
                        unit.timer = 1.25f + Range(0f, 0.35f);
                    }
                }
                else if (unit.kind == Kind.Heavy && unit.timer <= 0f)
                {
                    AimBullet(unit.pos + new Vector2(-0.25f, 0.25f), 2.45f, 0.08f);
                    AimBullet(unit.pos + new Vector2(-0.25f, -0.25f), 2.45f, -0.08f);
                    unit.timer = 1.9f + Range(0f, 0.5f);
                }
                else if (unit.kind == Kind.Diver)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 3.0f + unit.phase) * 2.0f * dt;
                    if (unit.timer <= 0f)
                    {
                        AimBullet(unit.pos, 3.0f, unit.phase > 1f ? -.24f : .24f);
                        unit.timer = 1.15f + Range(0f, .25f);
                    }
                }
                else if (unit.kind == Kind.Mine)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 1.4f + unit.phase) * .52f * dt;
                    if (unit.timer <= 0f)
                    {
                        unit.timer = 1.7f + Range(0f, .35f);
                        RingVolley(unit.pos, 4, 2.15f, .13f);
                    }
                }
                else if (unit.kind == Kind.Turret)
                {
                    unit.pos.y += Mathf.Sin(unit.age * .9f + unit.phase) * .22f * dt;
                    if (unit.timer <= 0f)
                    {
                        AimBullet(unit.pos + Vector2.left * .25f, 3.0f, 0f);
                        unit.timer = 1.55f + Range(0f, .45f);
                    }
                }
                else if (unit.kind == Kind.Winder)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 4.5f + unit.phase) * 1.35f * dt;
                    unit.pos.x += Mathf.Sin(unit.age * 2.1f + unit.phase) * .38f * dt;
                    if (unit.timer <= 0f)
                    {
                        FanVolley(unit.pos, 3, 2.7f, .24f);
                        unit.timer = 1.75f;
                    }
                }
                else if (unit.kind == Kind.Splitter)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 1.9f + unit.phase) * .82f * dt;
                    if (unit.timer <= 0f)
                    {
                        AimBullet(unit.pos, 2.75f, 0f);
                        unit.timer = 1.4f;
                    }
                }
                else if (unit.kind == Kind.Shield)
                {
                    unit.pos.y += Mathf.Sin(unit.age * 1.1f + unit.phase) * .5f * dt;
                    unit.guard = .5f + .5f * Mathf.Sin(unit.age * 1.6f + unit.phase);
                    unit.guardOpen = unit.guard > .62f;
                    if (unit.timer <= 0f)
                    {
                        FanVolley(unit.pos + Vector2.left * .3f, 2, 2.5f, .18f);
                        unit.timer = 2.0f;
                    }
                }
                else if (unit.kind == Kind.Lancer)
                {
                    if (!unit.committed)
                    {
                        if (unit.timer <= 0f)
                        {
                            // Commit once at the end of the telegraph. The direction is
                            // fixed for the whole lunge, so a moving player can sidestep it.
                            unit.committed = true;
                            unit.actionTimer = .58f;
                            unit.vel = (player - unit.pos).normalized * 5.2f;
                            unit.telegraph = 0f;
                        }
                        else
                        {
                            unit.pos.y += Mathf.Sin(unit.age * 1.2f + unit.phase) * .35f * dt;
                        }
                    }
                    else
                    {
                        unit.actionTimer -= dt;
                        if (unit.actionTimer <= 0f)
                        {
                            unit.committed = false;
                            unit.timer = 1.40f;
                            unit.vel = Vector2.left * 1.35f;
                        }
                    }
                }

                if (unit.pos.x < WorldMinX - 0.8f || unit.pos.x > WorldMaxX + 0.8f ||
                    unit.pos.y < WorldMinY - 0.9f || unit.pos.y > WorldMaxY + 0.9f)
                    units.RemoveAt(index);
            }
        }

        private void PrimeTelegraph(Unit unit)
        {
            float window = unit.kind == Kind.Lancer ? 1.05f : unit.kind == Kind.Boss ? .42f : .28f;
            if (unit.timer > 0f && unit.timer < window)
                unit.telegraph = Mathf.Clamp01(1f - unit.timer / window);
            else if (unit.timer <= 0f && !unit.committed)
                unit.telegraph = 0f;
        }

        private void FirePlayerShot(float dt)
        {
            shotTimer -= dt;
            int emitted = 0;
            float interval = pilot == 0 ? .092f : pilot == 1 ? .165f : .235f;
            int maxShots = pilot == 1 ? 3 : 1;
            while (shotTimer <= 0f && emitted < maxShots)
            {
                if (pilot == 0)
                {
                    AddPlayerShot(0f, 16.8f, 1, .145f);
                }
                else if (pilot == 1)
                {
                    AddPlayerShot(-.18f, 14.3f, 1, .145f);
                    AddPlayerShot(0f, 15.2f, 1, .16f);
                    AddPlayerShot(.18f, 14.3f, 1, .145f);
                }
                else
                {
                    AddPlayerShot(0f, 17.2f, 5, .28f);
                }
                Emit("fire", player, combo);
                shotTimer += interval;
                emitted++;
            }
        }

        private void AddPlayerShot(float angle, float speed, int damage, float radius)
        {
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Unit shot = AddProjectile(Kind.Shot, player + new Vector2(.56f, 0f), direction * speed, radius);
            shot.damage = damage;
            shot.variant = pilot;
        }

        private void ResolvePlayerHits()
        {
            for (int index = units.Count - 1; index >= 0; index--)
            {
                Unit unit = units[index];
                if (unit.kind == Kind.Bullet)
                {
                    if (Within(unit.pos, player, unit.radius + PlayerBulletRadius))
                    {
                        units.RemoveAt(index);
                        DamagePlayer(1, player);
                    }
                }
                else if (IsEnemy(unit) && Within(unit.pos, player, unit.radius + 0.32f))
                {
                    DamagePlayer(unit.kind == Kind.Boss ? 2 : 1, player);
                    if (unit.kind != Kind.Boss)
                        unit.hp = 0;
                }
            }
        }

        private void ResolveShotHits()
        {
            for (int shotIndex = units.Count - 1; shotIndex >= 0; shotIndex--)
            {
                Unit shot = units[shotIndex];
                if (shot.kind != Kind.Shot)
                    continue;

                for (int enemyIndex = 0; enemyIndex < units.Count; enemyIndex++)
                {
                    Unit enemy = units[enemyIndex];
                    if (!IsEnemy(enemy) || !Within(shot.pos, enemy.pos, shot.radius + enemy.radius))
                        continue;

                    int dealt = shot.damage;
                    if (enemy.kind == Kind.Boss)
                        dealt = Mathf.Max(1, dealt / 2);
                    else if (enemy.kind == Kind.Shield && !enemy.guardOpen)
                    {
                        // Akari and Mio's single-power shots are visibly blocked while
                        // the shield is closed. Sena's lance still chips the armor.
                        if (shot.damage <= 1)
                        {
                            units.RemoveAt(shotIndex);
                            Emit("block", enemy.pos, 0);
                            break;
                        }
                        dealt = Mathf.Max(1, dealt / 3);
                    }
                    enemy.hp -= dealt;
                    if (enemy.kind == Kind.Boss)
                        AddCharge(.03f + .015f * shot.damage);
                    units.RemoveAt(shotIndex);
                    Emit("hit", enemy.pos, combo);
                    break;
                }
            }
        }

        private void ResolveEnemyDeaths()
        {
            for (int index = units.Count - 1; index >= 0; index--)
            {
                Unit unit = units[index];
                if (!IsEnemy(unit) || unit.hp > 0)
                    continue;

                units.RemoveAt(index);
                if (unit.kind == Kind.Boss)
                {
                    bossRatio = 0f;
                    bossBreak = unit.pos;
                    score += 5000;
                    Emit("kill", unit.pos, combo + 1);
                    continue;
                }

                if (unit.kind == Kind.Splitter)
                {
                    // The split is deliberately small and slow, so it rewards finishing a
                    // splitter during a chain without turning the screen into a surprise.
                    AddEnemy(Kind.Scout, unit.pos + new Vector2(0f, .24f), new Vector2(-2.45f, .4f), 1, 0f);
                    AddEnemy(Kind.Scout, unit.pos + new Vector2(0f, -.24f), new Vector2(-2.45f, -.4f), 1, 0f);
                }

                kills++;
                combo = comboTime > 0f ? combo + 1 : 1;
                comboTime = ComboWindow;
                maxCombo = Mathf.Max(maxCombo, combo);
                int value = unit.kind == Kind.Heavy ? 300 : unit.kind == Kind.Shield ? 340 :
                    unit.kind == Kind.Turret ? 230 : unit.kind == Kind.Splitter ? 260 :
                    unit.kind == Kind.Lancer ? 220 : unit.kind == Kind.Winder ? 210 :
                    unit.kind == Kind.Diver ? 170 : unit.kind == Kind.Mine ? 120 :
                    unit.kind == Kind.Gunner ? 180 : 100;
                score += value * Mathf.Clamp(combo, 1, 8);
                Emit("kill", unit.pos, combo);
                AddCharge(unit.kind == Kind.Heavy ? 0.22f : 0.14f);
            }
        }

        private void TriggerBurst()
        {
            charge = 0f;
            bursts++;

            int erased = 0;
            for (int index = units.Count - 1; index >= 0; index--)
            {
                Unit unit = units[index];
                if (unit.kind == Kind.Bullet)
                {
                    units.RemoveAt(index);
                    erased++;
                }
                else if (IsEnemy(unit))
                {
                    unit.hp -= unit.kind == Kind.Boss ? 30 : 99;
                }
            }

            Emit("burst", player, erased);
        }

        private void AddCharge(float amount)
        {
            bool wasFull = charge >= 1f;
            charge = Mathf.Clamp01(charge + amount);
            if (!wasFull && charge >= 1f) Emit("ready", player, combo);
        }

        private void DamagePlayer(int damage, Vector2 at)
        {
            if (invuln > 0f || phase != Phase.Run)
                return;

            hull = Mathf.Max(0, hull - damage);
            invuln = 1.2f;
            combo = 0;
            comboTime = 0f;
            Emit("hurt", at, hull);
            if (hull <= 0)
            {
                phase = Phase.Defeat;
                Emit("defeat", player, score);
            }
        }

        private void AimBullet(Vector2 origin, float speed, float angleOffset)
        {
            Vector2 direction = player - origin;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.left;
            direction.Normalize();
            float radians = angleOffset;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            Vector2 aimed = new Vector2(direction.x * cosine - direction.y * sine, direction.x * sine + direction.y * cosine);
            AddProjectile(Kind.Bullet, origin, aimed * speed, 0.19f);
        }

        private void UpdateBoss(Unit boss, float dt)
        {
            float phaseSpeed = boss.hp < boss.maxHp / 2 ? 1.18f : .9f;
            PrimeTelegraph(boss);
            bool entering = boss.pos.x > 6.15f;
            bool holding = boss.actionTimer > 0f;
            if (holding)
                boss.actionTimer -= dt;
            if (entering)
            {
                // All bosses enter from the right edge at a readable pace. Their
                // combat movement starts only once they reach the arena lane.
                boss.pos.x = Mathf.MoveTowards(boss.pos.x, 5.35f, 1.55f * dt);
                boss.pos.y = Mathf.MoveTowards(boss.pos.y, 0f, 1.2f * dt);
            }
            else if (!holding)
            {
                switch (chapter)
                {
                    case 0: // Manta-01, a readable sine glide.
                        boss.pos.y = Mathf.Sin(boss.age * phaseSpeed) * 2.15f;
                        break;
                    case 1: // Leviathan, a rail engine that sweeps between high/low tracks.
                        boss.pos.y = Mathf.MoveTowards(boss.pos.y, Mathf.Sin(boss.age * .62f) * 2.75f, 2.4f * dt);
                        break;
                    case 2: // Orchid Queen, a hanging flower with slow vertical petals.
                        boss.pos.y = Mathf.Sin(boss.age * .52f) * 1.75f + Mathf.Sin(boss.age * 1.41f) * .4f;
                        boss.pos.x = 5.55f + Mathf.Sin(boss.age * .8f) * .45f;
                        break;
                    case 3: // Gemini, figure-eight movement around a center line.
                        boss.pos.y = Mathf.Sin(boss.age * .88f) * 2.45f;
                        boss.pos.x = 5.20f + Mathf.Cos(boss.age * .44f) * .52f;
                        break;
                    default: // Coda, stationary conductor with a dangerous phase change.
                        boss.pos.y = Mathf.Sin(boss.age * .65f) * 1.22f;
                        boss.pos.x = 5.42f + Mathf.Sin(boss.age * .37f) * .34f;
                        break;
                }
            }

            if (boss.timer <= 0f)
            {
                boss.telegraph = 0f;
                BossVolley(boss);
                float fast = boss.hp < boss.maxHp / 2 ? .82f : 1.12f;
                boss.timer = chapter == 4 ? fast * .9f : chapter == 1 ? fast * 1.08f : fast;
            }
        }

        private void BossVolley(Unit boss)
        {
            boss.pattern++;
            bool secondPattern = boss.hp <= boss.maxHp / 2;
            switch (chapter)
            {
                case 0: // Manta-01: aimed triple burst with a small gap between salvos.
                    AimBullet(boss.pos + Vector2.left, 3.4f, 0f);
                    AimBullet(boss.pos + Vector2.left, 3.15f, .25f);
                    AimBullet(boss.pos + Vector2.left, 3.15f, -.25f);
                    if (secondPattern)
                        FanVolley(boss.pos + Vector2.left, 4, 2.8f, .35f);
                    break;
                case 1: // Leviathan: HP-half phase adds readable horizontal rail lines.
                    if (secondPattern)
                        RailVolley(boss, 2.55f);
                    else
                    {
                        AimBullet(boss.pos + Vector2.left, 3.05f, 0f);
                        FanVolley(boss.pos + Vector2.left * .6f, 5, 2.55f, .13f);
                    }
                    break;
                case 2: // Orchid: HP-half phase blooms a petal vortex.
                    if (secondPattern)
                        PetalVortex(boss, 2.0f);
                    else
                    {
                        FanVolley(boss.pos + Vector2.left, 7, 2.45f, .20f);
                        RingVolley(boss.pos + Vector2.left * .3f, 3, 1.85f, .30f);
                    }
                    break;
                case 3: // Gemini: HP-half phase fires from twin points.
                    if (secondPattern)
                        TwinVolley(boss, 3.0f);
                    else
                    {
                        AimBullet(boss.pos + Vector2.left, 3.5f, 0f);
                        FanVolley(boss.pos + Vector2.left, 3, 3.0f, .32f);
                        RingVolley(boss.pos, 2, 2.1f, .36f);
                    }
                    break;
                default: // Coda: every fourth beat holds still and conducts a large fan.
                    if (secondPattern && boss.pattern % 4 == 0)
                    {
                        boss.actionTimer = .42f;
                        FanVolley(boss.pos + Vector2.left, 9, 2.65f, .17f);
                        RingVolley(boss.pos, 4, 1.95f, .28f);
                    }
                    else
                    {
                        AimBullet(boss.pos + Vector2.left, 3.6f, 0f);
                        FanVolley(boss.pos + Vector2.left, 5, 2.85f, .22f);
                    }
                    break;
            }
        }

        private void FanVolley(Vector2 origin, int count, float speed, float angleStep)
        {
            if (count <= 0)
                return;
            float center = (count - 1) * .5f;
            for (int i = 0; i < count; i++)
                AimBullet(origin, speed, (i - center) * angleStep);
        }

        private void RailVolley(Unit boss, float speed)
        {
            // Three rails leave two lane gaps. They travel horizontally so the warning
            // remains legible as the Leviathan changes track.
            float[] offsets = { -.96f, 0f, .96f };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 origin = boss.pos + Vector2.left * .8f + Vector2.up * offsets[i];
                AddProjectile(Kind.Bullet, origin, Vector2.left * speed, .17f);
            }
        }

        private void PetalVortex(Unit boss, float speed)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.PI * 2f * i / 8f + boss.age * .16f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddProjectile(Kind.Bullet, boss.pos + Vector2.left * .6f, direction * speed, .16f);
            }
            FanVolley(boss.pos + Vector2.left, 3, 2.7f, .27f);
        }

        private void TwinVolley(Unit boss, float speed)
        {
            Vector2 upper = boss.pos + Vector2.up * .65f + Vector2.left * .55f;
            Vector2 lower = boss.pos + Vector2.down * .65f + Vector2.left * .55f;
            AimBullet(upper, speed, -.20f);
            AimBullet(upper, speed, .08f);
            AimBullet(lower, speed, .20f);
            AimBullet(lower, speed, -.08f);
        }

        private void RingVolley(Vector2 origin, int count, float speed, float angleOffset)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count + angleOffset;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddProjectile(Kind.Bullet, origin, direction * speed, .17f);
            }
        }

        private void UpdateBossRatio()
        {
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.kind == Kind.Boss)
                {
                    bossRatio = Mathf.Clamp01((float)unit.hp / Mathf.Max(1, unit.maxHp));
                    return;
                }
            }
            bossRatio = 0f;
        }

        private static bool IsEnemy(Unit unit)
        {
            return unit.kind == Kind.Scout || unit.kind == Kind.Gunner || unit.kind == Kind.Heavy ||
                unit.kind == Kind.Diver || unit.kind == Kind.Mine || unit.kind == Kind.Turret ||
                unit.kind == Kind.Winder || unit.kind == Kind.Splitter || unit.kind == Kind.Shield ||
                unit.kind == Kind.Lancer || unit.kind == Kind.Boss;
        }

        private static bool Within(Vector2 a, Vector2 b, float radius)
        {
            return (a - b).sqrMagnitude <= radius * radius;
        }

        private void Emit(string name, Vector2 at, int value)
        {
            if (signal != null)
                signal(name, at, value);
        }

        private int NextInt(int minimum, int maximumExclusive)
        {
            randomState = randomState * 1664525u + 1013904223u;
            return minimum + (int)(randomState % (uint)(maximumExclusive - minimum));
        }

        private float Range(float minimum, float maximum)
        {
            randomState = randomState * 1664525u + 1013904223u;
            float unit = (randomState & 0x00FFFFFFu) / 16777215f;
            return Mathf.Lerp(minimum, maximum, unit);
        }
    }
}
