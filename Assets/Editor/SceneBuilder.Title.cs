using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmActionGame.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>
        /// 작업 R35: 시작화면 지붕 달리기. 가운데 줄 기와집(giwa_long, 긴 축을 달리는 방향 z로) RidgeCount채를 틈을 두고 잇고,
        /// 양옆에 낮은 기와집(giwa_s) 줄, 해 질 녘 해·안개·하늘·먼 배경 원통, 주인공(달리기 동작만)을 만든다. 움직임은 TitleRunner(러닝머신).
        /// </summary>
        /// <summary>감싼 모델을 게임 맵과 같은 크기(GameConfig.Map.Modules: 길이 가운데 값·깊이·높이)로 키우고 그 크기를 돌려준다.</summary>
        static Vector3 FitModule(Wrapped w, string id)
        {
            var m = System.Array.Find(GameConfig.Map.Modules, x => x.Id == id);
            var want = new Vector3((m.WidthMin + m.WidthMax) * 0.5f, m.Height, m.Depth);
            w.Go.transform.localScale = new Vector3(want.x / Mathf.Max(1e-4f, w.Size.x), want.y / Mathf.Max(1e-4f, w.Size.y), want.z / Mathf.Max(1e-4f, w.Size.z));
            return want;
        }

        static void BuildTitleRun(Camera cam)
        {
            var sun = new GameObject("Sun", typeof(Light));
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.shadows = LightShadows.Soft;
            BuildAtmosphere(light);

            var root = new GameObject("TitleRun").transform;
            // 먼 배경 원통 (게임과 같은 파노라마) — 주인공을 가운데에
            var backdropMat = AssetDatabase.LoadAssetAtPath<Material>($"{BackdropDir}/Backdrop.mat");
            if (backdropMat != null)
            {
                var bd = new GameObject("Backdrop", typeof(MeshFilter), typeof(MeshRenderer));
                bd.transform.SetParent(root, false);
                bd.transform.position = new Vector3(0f, -GameConfig.Map.BackdropSink, 0f);
                // 게임 씬 빌드가 BackdropCylinder.asset을 지우고 다시 만들므로 (참조가 끊김) 시작화면은 따로 둔다
                string meshPath = $"{BackdropDir}/TitleBackdropCylinder.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null) { mesh = Cylinder(96, GameConfig.Map.BackdropRadius, GameConfig.Map.BackdropHeight); AssetDatabase.CreateAsset(mesh, meshPath); }
                bd.GetComponent<MeshFilter>().sharedMesh = mesh;
                var mr = bd.GetComponent<MeshRenderer>();
                mr.sharedMaterial = backdropMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            // 골목 바닥 (무늬 없는 한 장 — 움직이지 않아도 흐르는 지붕과 어긋나 보이지 않는다)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -GameConfig.Title.SideDrop - 0.05f, 40f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<MeshRenderer>().sharedMaterial = Mat("TitleGround", new Color(0.42f, 0.33f, 0.26f));

            // 가운데 줄: 긴 축(Wrap의 x)을 z로 돌려 달리는 방향을 따라 잇는다
            var ridges = new List<Transform>();
            float len = 0f, top = 0f;
            for (int i = 0; i < GameConfig.Title.RidgeCount; i++)
            {
                var w = Wrap("giwa_long", root, $"Ridge_{i}");
                var sz = FitModule(w, "giwa_long");
                len = sz.x; top = sz.y;
                foreach (var t in w.Go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;   // 움직이는 조각
                w.Go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                ridges.Add(w.Go.transform);
            }
            float period = len + GameConfig.Title.RidgeGap;
            for (int i = 0; i < ridges.Count; i++)
                ridges[i].position = new Vector3(0f, 0f, -GameConfig.Title.RecycleBehind + len * 0.5f + i * period);

            // 양옆 낮은 지붕 줄
            var sides = new List<Transform>();
            float sideLen = 0f;
            foreach (float x in GameConfig.Title.SideRowX)
                for (int i = 0; i < GameConfig.Title.SideCount; i++)
                {
                    var w = Wrap("giwa_s", root, $"Side_{x:0}_{i}");
                    sideLen = FitModule(w, "giwa_s").x;
                    foreach (var t in w.Go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;
                    w.Go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    sides.Add(w.Go.transform);
                }
            float sidePeriod = sideLen + GameConfig.Title.SideGap;
            int k = 0;
            foreach (float x in GameConfig.Title.SideRowX)
                for (int i = 0; i < GameConfig.Title.SideCount; i++, k++)
                {
                    float stagger = (Mathf.Abs(x) > 10f ? 0.5f : 0f) * sidePeriod;   // 바깥 줄은 반 칸 어긋나게
                    sides[k].position = new Vector3(x, -GameConfig.Title.SideDrop, -GameConfig.Title.RecycleBehind + i * sidePeriod + stagger);
                }

            // 주인공 (달리기 동작만, PlayerBody 없이)
            var player = new GameObject("TitlePlayer").transform;
            player.SetParent(root, false);
            player.position = new Vector3(0f, top, 0f);
            ToonMaterials.BuildAll();
            var ctrl = ToonMaterials.BuildPlayerController(out _, out _);
            string fbxPath = $"{HumanoidImport.Root}/player/{BodyMoves.RunClip}.fbx";
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.SetParent(player, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            float h = MeasureHeight(smr, out float minY);
            if (h > 0.01f)
            {
                float s = GameConfig.Body.PlayerHeight / h;
                model.transform.localScale *= s;
                model.transform.localPosition = new Vector3(0f, -(minY - player.position.y) * s, 0f);
            }
            smr.sharedMaterials = new[] { ToonMaterials.Character("player") };
            smr.updateWhenOffscreen = true;
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var runner = root.gameObject.AddComponent<TitleRunner>();
            runner.player = player;
            runner.animator = animator;
            runner.cam = cam.transform;
            runner.ridge = ridges.ToArray();
            runner.ridgeLength = len;
            runner.ridgeTop = top;
            runner.sides = sides.ToArray();
            runner.sidePeriod = sidePeriod;
            cam.transform.position = player.position + GameConfig.Title.CamOffset;
            cam.transform.LookAt(player.position + GameConfig.Title.CamLook);
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, GameConfig.Map.BackdropRadius * 1.2f);
        }
    }
}
