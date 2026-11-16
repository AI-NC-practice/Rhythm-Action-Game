using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 적 모델·Animator·손에 든 무기 (작업 T14).
    /// - Enemy_&lt;model&gt;.controller: Idle, Attack_*, Guard, Hit, Defeat_* (EnemyMoves).
    /// - 타격 순간: 공격 클립마다 오른손 속도가 가장 빠른 때 (앞 HitSearchSkip 비율은 빼고, HitSearchSeconds 안).
    /// - 무기: weapon GLB를 오른손 뼈 아래 WeaponGrip에 붙인다. 손잡이·날 끝은 메시 모양으로 찾는다
    ///   (칼: 가장 넓은 코등이 쪽이 손잡이, 창: 가장 넓은 창날 쪽이 끝). 날은 대기 자세에서 몸 앞을 향한다.
    /// </summary>
    public static class EnemyRig
    {
        public const string Dir = "Assets/Art/Animations";

        public class Built
        {
            public AnimatorController Controller;
            public string[] Names;
            public float[] Lengths;
            public float[] Hits;
        }

        static readonly Dictionary<string, Built> cache = new Dictionary<string, Built>();

        public static void ClearCache() => cache.Clear();

        public static string ControllerPath(string model) => $"{Dir}/Enemy_{model}.controller";

        static string FbxPath(string model, string clip) => $"{HumanoidImport.Root}/{model}/{model}_{clip}.fbx";

        static AnimationClip Clip(string model, string clip)
        {
            if (clip.StartsWith(EnemyMoves.MixamoPrefix))   // 작업 R07: Mixamo 동작 (Humanoid)
            {
                string mp = $"{HumanoidImport.MixamoRoot}/{clip.Substring(EnemyMoves.MixamoPrefix.Length)}.fbx";
                var mc = AssetDatabase.LoadAllAssetsAtPath(mp).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
                if (mc == null) Debug.LogError($"[EnemyRig] 클립 없음: {mp}");
                return mc;
            }
            var c = AssetDatabase.LoadAllAssetsAtPath(FbxPath(model, clip)).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
            if (c == null) Debug.LogError($"[EnemyRig] 클립 없음: {FbxPath(model, clip)}");
            return c;
        }

        /// <summary>메시가 든 기본 FBX (첫 동작 = 대기).</summary>
        /// <summary>모델(메시·아바타) FBX — 작업 R27 보강: VARCO "humanoid-fingers"로 다시 리깅한 것 (손가락 뼈가 있어 칼자루를 쥘 수 있다).</summary>
        public static string BaseFbx(string model) => FbxPath(model, "rig_fingers");

        public static Built Controller(string model)
        {
            if (cache.TryGetValue(model, out var b)) return b;
            Directory.CreateDirectory(Dir);
            string path = ControllerPath(model);
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ctrl.layers[0].stateMachine;
            var states = new List<(string name, AnimationClip clip)>
            {
                (EnemyMoves.Idle, Clip(model, EnemyMoves.IdleClip(model))),
                (EnemyMoves.Guard, Clip(model, EnemyMoves.GuardClip(model))),
                (EnemyMoves.Hit, Clip(model, EnemyMoves.HitClip(model))),
                (EnemyMoves.Run, Clip(EnemyMoves.RunClipModel(model), EnemyMoves.RunClip)),   // 작업 R07
                (EnemyMoves.Walk, Clip(model, EnemyMoves.WalkClip)),   // 작업 R18 후속
                (EnemyMoves.LeapJump, Clip(model, EnemyMoves.LeapJumpClip)),   // 작업 R18c
                (EnemyMoves.LeapFall, Clip(model, EnemyMoves.LeapFallClip)),
                (EnemyMoves.LeapLand, Clip(model, EnemyMoves.LeapLandClip)),
            };
            foreach (var a in EnemyMoves.Attacks) states.Add((EnemyMoves.AttackState(a), Clip(model, EnemyMoves.AttackClip(model, a))));
            foreach (var d in EnemyMoves.Defeats) states.Add((EnemyMoves.DefeatState(d), Clip(model, EnemyMoves.DefeatClip(model, d))));
            int i = 0;
            foreach (var (name, clip) in states)
            {
                var st = sm.AddState(name, new Vector3(300 + (i % 3) * 220, (i / 3) * 70, 0));
                st.motion = clip;
                if (name == EnemyMoves.Idle) sm.defaultState = st;
                i++;
            }
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            b = new Built
            {
                Controller = ctrl,
                Names = states.Select(s => s.name).ToArray(),
                Lengths = states.Select(s => s.clip != null ? s.clip.length : 1f).ToArray(),
            };
            b.Hits = FindHits(model, ctrl, b.Names, b.Lengths);
            cache[model] = b;
            return b;
        }

        /// <summary>공격 상태마다 오른손이 가장 빠른 시각(초). 공격이 아닌 상태는 0.</summary>
        static float[] FindHits(string model, AnimatorController ctrl, string[] names, float[] lengths)
        {
            var hits = new float[names.Length];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx(model)));
            try
            {
                var a = go.GetComponent<Animator>();
                a.runtimeAnimatorController = ctrl;
                a.applyRootMotion = false;
                var log = new System.Text.StringBuilder();
                // 작업 R18c: 도약은 두 발이 모두 땅(클립 최저 높이 + 12 cm)에서 처음 떨어지는 순간, 착지는 공중 뒤 처음 닿는 순간
                var footL = a.GetBoneTransform(HumanBodyBones.LeftFoot);
                var footR = a.GetBoneTransform(HumanBodyBones.RightFoot);
                for (int k = 0; k < names.Length; k++)
                {
                    if (names[k] != EnemyMoves.LeapJump && names[k] != EnemyMoves.LeapLand) continue;
                    var clip = (AnimationClip)ctrl.layers[0].stateMachine.states.First(x => x.state.name == names[k]).state.motion;
                    if (clip == null) continue;
                    const float st = 1f / 60f;
                    var ys = new List<float>();
                    for (float t = 0f; t <= lengths[k] + 1e-4f; t += st) { clip.SampleAnimation(go, t); ys.Add(Mathf.Min(footL.position.y, footR.position.y)); }
                    float ground = ys.Min();
                    int i0 = 0;
                    if (names[k] == EnemyMoves.LeapJump)
                    {
                        // 가장 긴 공중 구간(두 발이 모두 땅 + 12 cm 위)의 시작 = 박차는 순간 (달리기 걸음의 짧은 뜀은 무시)
                        int bestStart = 0, bestLen = 0, runStart = -1;
                        for (int j = 0; j <= ys.Count; j++)
                        {
                            bool air = j < ys.Count && ys[j] > ground + 0.12f;
                            if (air && runStart < 0) runStart = j;
                            if (!air && runStart >= 0) { if (j - runStart > bestLen) { bestLen = j - runStart; bestStart = runStart; } runStart = -1; }
                        }
                        i0 = bestStart;
                    }
                    else
                    {
                        while (i0 < ys.Count && ys[i0] <= ground + 0.12f) i0++;              // 시작이 땅이면 지나감
                        while (i0 < ys.Count && ys[i0] > ground + 0.03f) i0++;               // 공중 → 처음 닿음
                    }
                    hits[k] = Mathf.Min(i0, ys.Count - 1) * st;
                    Debug.Log($"[EnemyRig] {model} {names[k]} 열쇠 시각 {hits[k]:0.00}s / {lengths[k]:0.00}s (발 최저 {ground:0.00} m)");
                }
                for (int k = 0; k < names.Length; k++)
                {
                    if (!names[k].StartsWith("Attack_")) continue;
                    float len = lengths[k];
                    var clip = (AnimationClip)ctrl.layers[0].stateMachine.states.First(x => x.state.name == names[k]).state.motion;
                    float window = Mathf.Min(len, GameConfig.EnemyBody.HitSearchSeconds);
                    float from = window * GameConfig.EnemyBody.HitSearchSkip, to = window;
                    const float step = 1f / 60f;
                    Vector3? prev = null;
                    float best = -1f, bestT = from;
                    var hand = a.GetBoneTransform(HumanBodyBones.RightHand);
                    var hips = a.GetBoneTransform(HumanBodyBones.Hips);
                    for (float t = from; t <= to + 1e-4f; t += step)
                    {
                        clip.SampleAnimation(go, t);
                        var p = hips.InverseTransformPoint(hand.position);   // 몸 기준 손 위치 → 몸이 도는 것과 무관
                        if (prev.HasValue)
                        {
                            float v = (p - prev.Value).magnitude / step;
                            if (v > best) { best = v; bestT = t; }
                        }
                        prev = p;
                    }
                    hits[k] = bestT;
                    log.Append($"{names[k]}={bestT:0.00}s/{len:0.00}s (손 {best:0.0} m/s) ");
                }
                Debug.Log($"[EnemyRig] {model} 타격 순간: {log}");
            }
            finally { Object.DestroyImmediate(go); }
            return hits;
        }

        /// <summary>
        /// 적 모델을 root 아래 Model로 넣는다 (키 height, 툰 머티리얼, Animator, EnemyAnimator).
        /// 오른손에 무기를 붙이고 무기 끝(트레일·판정 원이 따라가는 점)을 돌려준다.
        /// </summary>
        public static EnemyAnimator Build(GameObject root, string kind, float height, out Transform weaponVisual, out Transform tip)
        {
            var built = Controller(EnemyMoves.ModelOf(kind));
            var animator = BuildModel(root, kind, height, out weaponVisual, out tip);
            var ea = root.AddComponent<EnemyAnimator>();
            ea.animator = animator;
            ea.stateNames = built.Names;
            ea.stateSeconds = built.Lengths;
            ea.hitSeconds = built.Hits;
            return ea;
        }

        /// <summary>사건 소품의 적 (돌진·매복·추격·복선, EventProps): 모델 + 칼, Animator는 대기 반복을 스스로 재생.</summary>
        public static Transform Figure(Transform parent, string name, Vector3 feetLocal)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = feetLocal;
            root.transform.localRotation = Quaternion.Euler(0, 180f, 0);   // 플레이어 쪽을 봄
            var an = BuildModel(root, "sword", 1.75f, out _, out _);
            var run = root.AddComponent<EnemyFigureRun>();   // 작업 R07: 움직이면 달리기 (돌진·매복·추격)
            run.animator = an;
            return root.transform;
        }

        static Animator BuildModel(GameObject root, string kind, float height, out Transform weaponVisual, out Transform tip)
        {
            string model = EnemyMoves.ModelOf(kind);
            var built = Controller(model);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx(model)));
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Model";
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var animator = go.GetComponent<Animator>();
            animator.runtimeAnimatorController = built.Controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Play(EnemyMoves.Idle, 0, 0f);
            animator.Update(0f);
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            smr.sharedMaterials = new[] { ToonMaterials.Character(model) };
            smr.updateWhenOffscreen = true;
            // 키 맞추기 (대기 자세, 발바닥 0)
            float h = Measure(smr, root.transform, out float minY);
            if (h > 0.01f)
            {
                float k = height / h;
                go.transform.localScale *= k;
                go.transform.localPosition = new Vector3(0f, -minY * k, 0f);
                animator.Update(0f);
            }
            AttachWeapon(animator, root.transform, kind, out weaponVisual, out tip);
            animator.Play(EnemyMoves.Idle, 0, 0f);
            animator.Update(0f);
            var fg = root.AddComponent<FingerGrip>();   // 작업 R27 보강: 손가락으로 칼자루를 감아쥠
            fg.animator = animator;
            fg.leftHand = kind == "spear";            // 창은 두 손으로
            return animator;
        }

        static float Measure(SkinnedMeshRenderer smr, Transform root, out float minY)
        {
            var m = new Mesh();
            smr.BakeMesh(m, true);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var v in m.vertices)
            {
                float y = root.InverseTransformPoint(smr.transform.position + smr.transform.rotation * v).y;
                lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            Object.DestroyImmediate(m);
            minY = lo;
            return hi - lo;
        }

        /// <summary>무기를 오른손 뼈에 쥐게 붙인다 (손잡이 점 = 손바닥). 작업 R27: 플레이어 칼도 이것을 쓴다.</summary>
        internal static void AttachWeapon(Animator animator, Transform root, string kind, out Transform weaponVisual, out Transform tip)
        {
            string id = EnemyMoves.WeaponOf(kind);
            bool spear = id == "weapon_spear";
            float length = spear ? GameConfig.EnemyBody.SpearLength : GameConfig.EnemyBody.SwordLength;
            // 작업 R27 보강: T자 기본 자세(손바닥 아래)에서 붙인다 — 대기 자세에서 붙이면 칼이 손끝 연장선처럼 나왔다.
            animator.Rebind();
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var lower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

            var grip = new GameObject("WeaponGrip").transform;
            grip.SetParent(hand, false);
            var forearm = (hand.position - lower.position).normalized;
            // 칼자루는 팔뚝에 거의 수직으로 주먹 가운데를 가로지르고, 칼날은 엄지 쪽(T자에서 앞)으로 조금 위로
            var blade = Vector3.ProjectOnPlane(root.forward + root.up * GameConfig.EnemyBody.GripBladeLift, forearm).normalized;
            if (blade.sqrMagnitude < 1e-4f) blade = root.forward;
            var palm = Vector3.ProjectOnPlane(-root.up, forearm).normalized;   // T자에서 손바닥은 아래
            grip.position = hand.position + forearm * GameConfig.EnemyBody.GripAlongHand + palm * GameConfig.EnemyBody.GripPalmDepth;
            grip.rotation = Quaternion.LookRotation(blade, -forearm);

            var w = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{id}.glb"));
            PrefabUtility.UnpackPrefabInstance(w, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            w.name = "Prop_" + id;
            var mat = ToonMaterials.Static(id);
            foreach (var r in w.GetComponentsInChildren<MeshRenderer>(true)) { r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray(); }

            // 메시 모양: 가장 긴 축, 끝 방향, 손잡이 점 (무기 자기 좌표)
            w.transform.SetParent(null, false);
            w.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            w.transform.localScale = Vector3.one;
            var pts = new List<Vector3>();
            foreach (var mf in w.GetComponentsInChildren<MeshFilter>(true))
                foreach (var v in mf.sharedMesh.vertices) pts.Add(w.transform.InverseTransformPoint(mf.transform.TransformPoint(v)));
            var min = new Vector3(pts.Min(p => p.x), pts.Min(p => p.y), pts.Min(p => p.z));
            var max = new Vector3(pts.Max(p => p.x), pts.Max(p => p.y), pts.Max(p => p.z));
            var size = max - min;
            int ax = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            var axis = Vector3.zero; axis[ax] = 1f;
            float lo = min[ax], hi = max[ax], span = Mathf.Max(hi - lo, 1e-4f);
            const int bins = 24;
            var width = new float[bins];
            var center = (min + max) * 0.5f;
            foreach (var p in pts)
            {
                int bi = Mathf.Clamp((int)((p[ax] - lo) / span * bins), 0, bins - 1);
                var off = p - center; off[ax] = 0f;
                width[bi] = Mathf.Max(width[bi], off.magnitude);
            }
            int widest = System.Array.IndexOf(width, width.Max());
            bool widestNearLo = widest < bins / 2;
            // 칼: 넓은 쪽(코등이)이 손잡이 / 창: 넓은 쪽(창날)이 끝
            bool tipAtHi = spear ? !widestNearLo : widestNearLo;
            var tipDir = tipAtHi ? axis : -axis;
            float handleEnd = tipAtHi ? lo : hi;
            float guardPos = lo + (widest + 0.5f) / bins * span;
            float gripAlong = spear ? handleEnd + (tipAtHi ? 1 : -1) * 0.35f * span : (handleEnd + guardPos) * 0.5f;
            var gripLocal = center; gripLocal[ax] = gripAlong;
            float tipAlong = tipAtHi ? hi : lo;
            float scale = length / span;

            w.transform.SetParent(grip, false);
            float inherited = Mathf.Max(grip.lossyScale.x, 1e-4f);    // 손 뼈가 모델 키 맞추기 배율을 물려받는다
            var rot = Quaternion.FromToRotation(tipDir, Vector3.forward);
            w.transform.localRotation = rot;
            w.transform.localScale = Vector3.one * (scale / inherited);
            w.transform.localPosition = -(rot * (gripLocal * scale)) / inherited;
            weaponVisual = grip;

            tip = new GameObject("WeaponTip").transform;
            tip.SetParent(grip, false);
            tip.localPosition = Vector3.forward * Mathf.Abs(tipAlong - gripAlong) * scale / inherited;
        }
    }
}
