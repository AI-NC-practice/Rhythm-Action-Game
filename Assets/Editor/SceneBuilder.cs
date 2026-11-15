using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 씬을 코드로 만든다. 실행: harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.SceneBuilder.BuildAll
    /// 여러 번 실행해도 같은 결과 (씬을 새로 만들어 덮어쓴다).
    /// </summary>
    public static partial class SceneBuilder
    {
        public const string ScenesDir = "Assets/Scenes";
        public const string TitleScenePath = ScenesDir + "/Title.unity";
        public const string GameScenePath = ScenesDir + "/Game.unity";
        public const string MaterialsDir = "Assets/Art/Materials/Gray";

        [MenuItem("RhythmActionGame/Build All Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(ScenesDir);
            Directory.CreateDirectory(MaterialsDir);
            MusicTool.ApplyImportSettings();
            AudioProjectSettings.Apply();
            BuildTitle();
            BuildGame();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Title, Game 씬 생성 완료");
        }

        // ------------------------------------------------------------------ Title
        static void BuildTitle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera("TitleCamera", new Vector3(0, 1.6f, -5f), Quaternion.identity);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.1f);
            BuildTitleUi();
            EditorSceneManager.SaveScene(scene, TitleScenePath);
        }

        /// <summary>타이틀 화면 UI. T08에서 메뉴를 채운다.</summary>
        static partial void BuildTitleUiExtra(Canvas canvas);

        static void BuildTitleUi()
        {
            var canvas = UiFactory.CreateCanvas("TitleCanvas", 0);
            var title = UiFactory.CreateText(canvas.transform, "Title", "Rhythm-Action-Game", 96, Color.white, new Vector2(1600, 160));
            title.rectTransform.anchoredPosition = new Vector2(0, 220);
            // 음악 출처 (B10, CC BY): 화면 아래 가운데
            var credit = UiFactory.CreateText(canvas.transform, "MusicCredit", GameConfig.Music.Credit, 15, new Color(1f, 1f, 1f, 0.5f), new Vector2(1600, 28));
            credit.rectTransform.anchorMin = credit.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            credit.rectTransform.anchoredPosition = new Vector2(0, 24);
            BuildTitleUiExtra(canvas);
        }

        // ------------------------------------------------------------------ Game
        static void BuildGame()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var chart = StageChart.Load();

            // 조명
            var sun = new GameObject("Sun", typeof(Light));
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.42f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);

            var map = new GameObject("Map").transform;
            BuildGround(map);
            BuildSections(map);
            BuildRoofs(map);
            BuildAlley(map);
            BuildObstacles(map, chart);
            var eventProps = BuildEventProps(map, chart);
            var enemies = BuildEnemies(map, chart);

            // 시스템
            var systems = new GameObject("Systems");
            var music = systems.AddComponent<AudioSource>();
            music.playOnAwake = false;
            // 배경 음악 (B10, B16): 판정 타임라인을 따라 만든 곡. BeatClock이 박자 위치로 재생 위치를 맞춘다.
            music.clip = MusicTool.LoadClip();
            music.loop = true;
            music.volume = GameConfig.Music.Volume;
            // 음 높이 보정 (GDD-beta B15.1): 음악 AudioSource 바로 뒤에 붙어 OnAudioFilterRead로 1/s
            var pitchCorrector = systems.AddComponent<MusicPitchCorrector>();
            var clock = systems.AddComponent<BeatClock>();
            clock.music = music;
            clock.pitchCorrector = pitchCorrector;
            // 효과음 + 합성 장단 반주 (GDD-beta B7: 메트로놈 대신)
            var sfxGo = new GameObject("Sfx");
            sfxGo.transform.SetParent(systems.transform);
            var sfx = sfxGo.AddComponent<SfxPlayer>();
            sfx.clips = SfxSynthTool.Generate();
            sfx.musicSources.Add(music);
            var backingGo = new GameObject("JangguBacking");
            backingGo.transform.SetParent(systems.transform);
            var backing = backingGo.AddComponent<JangguBacking>();
            backing.clock = clock;
            backing.sfx = sfx;
            // 곡이 있으면 합성 반주는 끈다 (GameConfig.Music.KeepJangguBacking). 곡이 없으면 예전처럼 반주로 박자를 알린다.
            backing.enabled = music.clip == null || GameConfig.Music.KeepJangguBacking;
            var judge = systems.AddComponent<JudgeSystem>();
            judge.clock = clock;
            // 히트사운드 (GDD-beta B14): 누르는 순간 장구 소리
            var hitGo = new GameObject("HitSound");
            hitGo.transform.SetParent(systems.transform);
            var hitSound = hitGo.AddComponent<HitSoundPlayer>();
            hitSound.judge = judge;
            hitSound.sfx = sfx;

            // 플레이어
            var player = new GameObject("Player");
            var follower = player.AddComponent<PathFollower>();
            follower.clock = clock;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            var bodyRenderer = body.GetComponent<MeshRenderer>();
            bodyRenderer.sharedMaterial = Mat("Player", new Color(0.9f, 0.9f, 0.85f));
            bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // 1인칭: 몸은 그림자만 (모델 교체는 T13)
            // 칼 막대 (1인칭에서는 그림자만, 무사 결투 3인칭 컷에서 보임 — GDD-beta B11.5)
            var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Sword";
            Object.DestroyImmediate(sword.GetComponent<Collider>());
            sword.transform.SetParent(player.transform, false);
            sword.transform.localPosition = new Vector3(0.35f, 1.1f, 0.35f);
            sword.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            sword.transform.localScale = new Vector3(0.05f, 0.05f, 0.9f);
            var swordRenderer = sword.GetComponent<MeshRenderer>();
            swordRenderer.sharedMaterial = Mat("PlayerSword", new Color(0.75f, 0.75f, 0.78f));
            swordRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            var rig = new GameObject("CameraRig");
            rig.transform.SetParent(player.transform, false);
            rig.transform.localPosition = new Vector3(0, GameConfig.EyeHeight, 0);
            var motion = new GameObject("Motion");   // 장애물·전투 카메라 연출 (StageDirector)
            motion.transform.SetParent(rig.transform, false);
            var cam = CreateCamera("PlayerCamera", Vector3.zero, Quaternion.identity);
            var fxOffset = new GameObject("FxOffset");   // 연출 층 (CameraFx, GDD-beta B5)
            fxOffset.transform.SetParent(motion.transform, false);
            cam.transform.SetParent(fxOffset.transform, false);
            cam.fieldOfView = GameConfig.FovDefault;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.72f, 0.82f);
            player.transform.position = MapLayout.PointAt(0);

            // 연출과 UI
            var fx = systems.AddComponent<JudgeFx>();
            fx.judge = judge;
            fx.cam = cam;
            var labelCanvas = UiFactory.CreateCanvas("JudgeLabelCanvas", GameConfig.JudgeFxCanvasOrder);
            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            var ringView = canvas.gameObject.AddComponent<JudgeRingView>();
            ringView.judge = judge;
            ringView.clock = clock;
            ringView.worldCamera = cam;
            ringView.labelLayer = (RectTransform)labelCanvas.transform;
            ringView.additiveMaterial = UiAdditiveMat();

            var director = systems.AddComponent<StageDirector>();
            director.clock = clock;
            director.judge = judge;
            director.player = follower;
            director.motion = motion.transform;
            director.normalEnemies = enemies.Normal.ToArray();
            director.slowMotion = systems.AddComponent<SlowMotion>();
            BuildDirectorExtra(director, enemies);

            // 새 사건·안 멈추는 적·복선 소품 진행 (GDD-beta B2)
            var propDirector = systems.AddComponent<EventPropDirector>();
            propDirector.director = director;
            propDirector.player = follower;
            propDirector.propRoot = eventProps;

            // 연출 시스템 (GDD-beta B5)
            var cameraFx = systems.AddComponent<CameraFx>();
            cameraFx.fxOffset = fxOffset.transform;
            cameraFx.judgeFx = fx;
            var timeFx = systems.AddComponent<TimeFx>();
            timeFx.slowMotion = director.slowMotion;
            timeFx.clock = clock;
            var presentation = systems.AddComponent<PresentationDirector>();
            presentation.cameraFx = cameraFx;
            presentation.timeFx = timeFx;
            director.presentation = presentation;
            var screenGo = new GameObject("ScreenFx");
            screenGo.transform.SetParent(systems.transform, false);
            var screenFx = screenGo.AddComponent<ScreenFx>();
            screenFx.judgeFx = fx;
            screenFx.judge = judge;
            presentation.screenFx = screenFx;
            var vfxGo = new GameObject("Vfx");
            vfxGo.transform.SetParent(systems.transform, false);
            var flashCanvas = UiFactory.CreateCanvas("ScreenFlashCanvas", GameConfig.JudgeFxCanvasOrder);
            var vfxPool = vfxGo.AddComponent<VfxPool>();
            vfxPool.flashLayer = (RectTransform)flashCanvas.transform;
            vfxPool.worldCamera = cam;
            vfxPool.particleMaterial = FxParticleMat();
            presentation.vfxPool = vfxPool;
            presentation.sfx = sfx;
            var hooks = systems.AddComponent<PresentationHooks>();   // 게임 이벤트 → 연출 (B07)
            hooks.presentation = presentation;
            hooks.director = director;
            hooks.judge = judge;
            hooks.clock = clock;
            hooks.player = follower;
            hooks.vfx = vfxPool;

            // 무사 결투 1인칭 가상 카메라 (GDD-beta B11.5, Cinemachine)
            var duelCam = systems.AddComponent<DuelCamera>();
            duelCam.director = director;
            duelCam.judge = judge;
            duelCam.judgeFx = fx;
            duelCam.cam = cam;
            duelCam.fxOffset = fxOffset.transform;
            duelCam.playerBody = new Renderer[] { bodyRenderer, swordRenderer };

            var boot = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            boot.director = director;
            boot.clock = clock;
            boot.judge = judge;
            boot.ringView = ringView;
            boot.fx = fx;
            boot.player = follower;
            boot.playerCamera = cam;
            boot.presentation = presentation;
            boot.backing = backing;
            boot.hitSound = hitSound;

            BuildGameExtra(boot, chart);
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        /// <summary>이후 작업(T05~T08)이 Game 씬에 덧붙이는 자리.</summary>
        static partial void BuildGameExtra(GameBootstrap boot, ChartData chart);

        static Camera CreateCamera(string name, Vector3 pos, Quaternion rot)
        {
            var go = new GameObject(name, typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(pos, rot);
            var cam = go.GetComponent<Camera>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            return cam;
        }

        static void BuildGround(Transform map)
        {
            var ground = Box(map, "Ground", new Vector3(135f, -0.1f, 195f), new Vector3(420f, 0.2f, 520f), Quaternion.identity, Mat("Ground", new Color(0.55f, 0.5f, 0.42f)));
            ground.isStatic = true;
            // 지면 위 길 (폭 3 m)
            int i = 0;
            foreach (var r in MapLayout.SplitAtCorners(0, MapLayout.TotalLength))
                StripBox(map, $"Path_{i++}", r.x, r.y, 0.02f, GameConfig.PathWidth, 0.04f, 0f, Mat("Path", new Color(0.7f, 0.66f, 0.58f)));
        }

        static void BuildSections(Transform map)
        {
            var parent = new GameObject("Sections").transform;
            parent.SetParent(map);
            foreach (var s in MapLayout.Sections)
            {
                var go = new GameObject($"Section_{s.Number}");
                go.transform.SetParent(parent);
                go.transform.position = MapLayout.PointAt(s.StartBeat * GameConfig.MetersPerBeat);
            }
        }

        static void BuildRoofs(Transform map)
        {
            var parent = new GameObject("Roofs").transform;
            parent.SetParent(map);
            int n = 0;
            foreach (var roof in MapLayout.Roofs)
            {
                var color = roof.AssetId == "gate" ? new Color(0.55f, 0.25f, 0.2f) : roof.AssetId == "wall_stone" ? new Color(0.6f, 0.6f, 0.58f) : new Color(0.32f, 0.33f, 0.36f);
                foreach (var r in MapLayout.SplitAtCorners(roof.Start, Mathf.Min(roof.End, MapLayout.TotalLength + 5f)))
                    StripBox(parent, $"Prop_{roof.AssetId}_{n++}", r.x, r.y, roof.Height * 0.5f, 6f, roof.Height, 0f, Mat("Roof_" + roof.AssetId, color));
            }
        }

        static void BuildAlley(Transform map)
        {
            // 1구간 골목 양옆 담 (높이 3 m)
            var parent = new GameObject("Alley").transform;
            parent.SetParent(map);
            var m = Mat("AlleyWall", new Color(0.78f, 0.74f, 0.66f));
            float end = MapLayout.Sections[0].EndBeat * GameConfig.MetersPerBeat - 4f;
            StripBox(parent, "AlleyWall_L", 4f, end, 1.5f, 0.6f, 3f, -2.6f, m);
            StripBox(parent, "AlleyWall_R", 4f, end, 1.5f, 0.6f, 3f, 2.6f, m);
        }

        static void BuildObstacles(Transform map, ChartData chart)
        {
            var parent = new GameObject("Obstacles").transform;
            parent.SetParent(map);
            int i = 0;
            foreach (var e in chart.Obstacles)
            {
                if (EventProps.Needs(e)) continue;   // 새 사건은 BuildEventProps
                float d = (e.runBeat + GameConfig.ObstacleBeats * 0.5f) * GameConfig.MetersPerBeat;
                var p = MapLayout.PointAt(d, out var fwd);
                var root = new GameObject($"Obstacle_{i++:00}_{e.kind}").transform;
                root.SetParent(parent);
                root.SetPositionAndRotation(p, Quaternion.LookRotation(fwd));
                var side = e.Key == ArrowKey.Left ? -1f : e.Key == ArrowKey.Right ? 1f : 0f;
                switch (e.kind)
                {
                    case "cart": LocalBox(root, "Prop_cart", new Vector3(0, 0.6f, 0), new Vector3(1.6f, 1.2f, 1.0f), Mat("Cart", new Color(0.55f, 0.38f, 0.22f))); break;
                    case "jars": LocalBox(root, "Prop_jars", new Vector3(side == 0 ? 0 : -side * 0.6f, 0.5f, 0), new Vector3(1.4f, 1.0f, 1.2f), Mat("Jars", new Color(0.45f, 0.28f, 0.18f))); break;
                    case "low_wall": LocalBox(root, "Prop_wall_stone", new Vector3(0, 0.5f, 0), new Vector3(3.2f, 1.0f, 0.5f), Mat("LowWall", new Color(0.6f, 0.6f, 0.58f))); break;
                    case "clothesline":
                        var cl = Mat("Clothesline", new Color(0.85f, 0.85f, 0.8f));
                        LocalBox(root, "Prop_clothesline_PostL", new Vector3(-1.7f, 1.0f, 0), new Vector3(0.1f, 2.0f, 0.1f), cl);
                        LocalBox(root, "Prop_clothesline_PostR", new Vector3(1.7f, 1.0f, 0), new Vector3(0.1f, 2.0f, 0.1f), cl);
                        LocalBox(root, "Prop_clothesline", new Vector3(0, 1.4f, 0), new Vector3(3.4f, 0.4f, 0.05f), cl);
                        break;
                    case "eave": LocalBox(root, "Prop_eave", new Vector3(0, 1.4f, 0), new Vector3(3.4f, 0.3f, 1.0f), Mat("Eave", new Color(0.3f, 0.3f, 0.33f))); break;
                    case "wall_run": LocalBox(root, "Prop_wall_stone", new Vector3(side * 2.0f, 1.75f, 0), new Vector3(0.5f, 3.5f, 8f), Mat("WallRun", new Color(0.66f, 0.64f, 0.6f))); break;
                    default: break; // roof_climb, roof_jump, drop_down, gate_climb: 지붕 덩어리가 장애물이다
                }
            }
        }

        /// <summary>새 사건·안 멈추는 적·복선의 회색 상자 소품 (GDD-beta B2.2, B2.3).</summary>
        static Transform BuildEventProps(Transform map, ChartData chart)
        {
            var parent = new GameObject("EventProps").transform;
            parent.SetParent(map);
            foreach (var e in chart.events)
                if (EventProps.Needs(e)) EventProps.Build(e, parent, Mat);
            return parent;
        }

        public class EnemySet
        {
            public readonly List<EnemyActor> Normal = new List<EnemyActor>();
            public readonly List<EnemyActor> Surround = new List<EnemyActor>();
            public EnemyActor Boss;
        }

        static EnemySet BuildEnemies(Transform map, ChartData chart)
        {
            var set = new EnemySet();
            var parent = new GameObject("Enemies").transform;
            parent.SetParent(map);
            int i = 0;
            foreach (var e in chart.events)
            {
                if (e.IsRunEvent || e.IsForeshadow) continue;   // 달리며 처리하는 사건·복선은 EventProps가 만든다
                var stop = MapLayout.PointAt(e.Distance, out var fwd);
                if (e.IsSurround)
                {
                    var ring = new GameObject("Surround").transform;
                    ring.SetParent(parent);
                    ring.position = stop;
                    for (int k = 0; k < GameConfig.SurroundEnemyCount; k++)
                    {
                        bool sword = k < GameConfig.SurroundSwordCount;
                        float ang = k * 360f / GameConfig.SurroundEnemyCount;
                        var dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
                        set.Surround.Add(Enemy(ring, $"Enemy_Surround_{k}_{(sword ? "sword" : "spear")}", sword ? "sword" : "spear", false, stop + dir * GameConfig.SurroundRadius, -dir, 1.75f, sword ? EnemyColorSword : EnemyColorSpear));
                    }
                    continue;
                }
                var pos = MapLayout.PointAt(e.Distance + GameConfig.EnemySpawnMeters);   // GDD-beta B13.2: 5 m 앞에서 돌진
                if (e.IsBoss) set.Boss = Enemy(parent, "Enemy_Boss", "boss", e.onRoof, pos, -fwd, 1.85f, new Color(0.45f, 0.08f, 0.08f));
                else set.Normal.Add(Enemy(parent, $"Enemy_{i++:00}_{e.kind}", e.kind, e.onRoof, pos, -fwd, 1.75f, e.kind == "spear" ? EnemyColorSpear : EnemyColorSword));
            }
            return set;
        }

        static readonly Color EnemyColorSword = new Color(0.8f, 0.2f, 0.2f);
        static readonly Color EnemyColorSpear = new Color(0.2f, 0.35f, 0.8f);

        /// <summary>T07이 포위 전투·무사 참조를 연결하는 자리.</summary>
        static partial void BuildDirectorExtra(StageDirector director, EnemySet enemies);

        /// <summary>적 하나: 발 위치 루트(EnemyActor) + 몸 캡슐 + 어깨 축 + 무기 + 무기 끝(트레일).</summary>
        static EnemyActor Enemy(Transform parent, string name, string kind, bool onRoof, Vector3 feet, Vector3 facing, float height, Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(new Vector3(facing.x, 0, facing.z)));

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * height * 0.5f;
            body.transform.localScale = new Vector3(0.6f, height * 0.5f, 0.6f);
            body.GetComponent<MeshRenderer>().sharedMaterial = Mat("Enemy_" + ColorUtility.ToHtmlStringRGB(color), color);

            var pivot = new GameObject("WeaponPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0.35f, height * 0.78f, 0.1f);
            float len = kind == "spear" ? 2.2f : 1.0f;
            var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = kind == "spear" ? "Prop_weapon_spear" : "Prop_weapon_sword";
            Object.DestroyImmediate(weapon.GetComponent<Collider>());
            weapon.transform.SetParent(pivot, false);
            weapon.transform.localPosition = new Vector3(0, 0, len * 0.5f);
            weapon.transform.localScale = new Vector3(0.06f, 0.06f, len);
            weapon.GetComponent<MeshRenderer>().sharedMaterial = Mat("Weapon", new Color(0.85f, 0.87f, 0.9f));
            var tip = new GameObject("WeaponTip").transform;
            tip.SetParent(pivot, false);
            tip.localPosition = new Vector3(0, 0, len);

            var ghost = GhostMat();
            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0f;
            trail.sharedMaterial = ghost;
            trail.emitting = false;
            trail.shadowCastingMode = ShadowCastingMode.Off;

            var actor = root.AddComponent<EnemyActor>();
            actor.kind = kind;
            actor.onRoof = onRoof;
            actor.weaponPivot = pivot;
            actor.weaponTip = tip;
            actor.trail = trail;
            actor.ghostMaterial = ghost;
            return actor;
        }

        /// <summary>잔상·트레일용 반투명 URP Unlit 머티리얼.</summary>
        static Material GhostMat()
        {
            const string path = "Assets/Art/Materials/Ghost.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetFloat("_Surface", 1f);   // Transparent
            m.SetFloat("_Blend", 0f);     // Alpha
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", new Color(0.75f, 0.9f, 1f, 0.45f));
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>경로 거리 [a, b] 를 따라 놓는 상자. centerY는 발 높이 기준이 아니라 월드 y(지면 0 기준).</summary>
        static GameObject StripBox(Transform parent, string name, float a, float b, float centerY, float width, float height, float sideOffset, Material m)
        {
            var pa = MapLayout.FlatPointAt(a, out var fwd);
            var pb = MapLayout.FlatPointAt(b - 0.001f, out _);
            var right = MapLayout.RightOf(fwd);
            var center = (pa + pb) * 0.5f + right * sideOffset;
            center.y = centerY;
            return Box(parent, name, center, new Vector3(width, height, Vector3.Distance(pa, pb)), Quaternion.LookRotation(fwd), m);
        }

        static GameObject LocalBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rot, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(center, rot);
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.isStatic = true;
            return go;
        }

        /// <summary>회색 상자용 URP Lit 머티리얼 (에셋으로 저장해 빌드에 포함).</summary>
        /// <summary>UI 가산 머티리얼 (GDD-beta B8.5). 에셋으로 두어 빌드에 셰이더가 들어가게 한다.</summary>
        static Material UiAdditiveMat()
        {
            string path = $"{MaterialsDir}/UIAdditive.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Directory.CreateDirectory(MaterialsDir);
                m = new Material(Shader.Find("RhythmActionGame/UIAdditive"));
                AssetDatabase.CreateAsset(m, path);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>이펙트 파티클 머티리얼 (URP Particles/Unlit 가산, GDD-beta B5). 에셋으로 두어 빌드에 셰이더가 들어가게 한다.</summary>
        static Material FxParticleMat()
        {
            string path = $"{MaterialsDir}/VfxParticle.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Directory.CreateDirectory(MaterialsDir);
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            VfxFactory.SetupAdditive(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Mat(string name, Color color)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Directory.CreateDirectory(MaterialsDir);
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
