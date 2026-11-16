using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmActionGame
{
    /// <summary>
    /// 길 옆 장식(집·담·항아리·수레)을 GPU 인스턴싱으로 그린다 (작업 T15, 드로우콜 줄이기).
    /// SceneBuilder가 (메시, 머티리얼, 그림자)마다 변환 행렬을 모아 넣는다. 같은 모델 수십 채가 패스마다 호출 한 번이 된다.
    /// 먼 줄은 그림자를 드리우지 않는다. 툰 셰이더는 multi_compile_instancing을 지원한다.
    /// 작업 R03: 맵을 GameConfig.Map.DressCell 칸으로 나눠 칸마다 묶고 경계를 주어, 보이지 않는 칸(뒤·그림자 거리 밖)은 그리지 않는다.
    /// </summary>
    public class DressingInstancer : MonoBehaviour
    {
        [System.Serializable]
        public class Group
        {
            public Mesh mesh;
            public int subMesh;
            public Material material;
            public bool castShadows;
            public Matrix4x4[] matrices;
            /// <summary>이 묶음 인스턴스 전체를 감싸는 월드 경계 (작업 R03). 카메라·그림자 밖이면 Unity가 이 호출을 건너뛴다. 크기 0이면 맵 전체.</summary>
            public Bounds bounds;
            /// <summary>카메라에서 이보다 먼 칸은 그리지 않는다 (m, 0이면 늘 그림). 작은 소품용 (작업 R03).</summary>
            public float maxDistance;
        }

        public List<Group> groups = new List<Group>();

        /// <summary>한 번에 그리는 최대 개수 (RenderMeshInstanced 제한).</summary>
        const int Chunk = 1023;

        public int InstanceCount { get { int n = 0; foreach (var g in groups) n += g.matrices != null ? g.matrices.Length : 0; return n; } }

        void Update() => Draw();

        /// <summary>이번 프레임에 그릴 인스턴스를 넣는다 (에디터 미리보기에서도 카메라 렌더 전에 부른다).</summary>
        public void Draw()
        {
            var cam = Camera.main;
            foreach (var g in groups)
            {
                if (g.mesh == null || g.material == null || g.matrices == null || g.matrices.Length == 0) continue;
                if (g.maxDistance > 0f && cam != null && g.bounds.SqrDistance(cam.transform.position) > g.maxDistance * g.maxDistance) continue;
                var rp = new RenderParams(g.material)
                {
                    shadowCastingMode = g.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    layer = gameObject.layer,
                    worldBounds = g.bounds.size.sqrMagnitude > 0f ? g.bounds : new Bounds(transform.position, Vector3.one * 2000f),
                };
                for (int start = 0; start < g.matrices.Length; start += Chunk)
                    Graphics.RenderMeshInstanced(rp, g.mesh, g.subMesh, g.matrices, Mathf.Min(Chunk, g.matrices.Length - start), start);
            }
        }
    }
}
