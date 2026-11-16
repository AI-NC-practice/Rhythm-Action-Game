using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>작업 R18 (GDD-beta B19): 추격 연출 개편은 판정을 바꾸지 않는다. 돌진 적 속도 곡선.</summary>
    public class R18_ChaseRedesignTests
    {
        /// <summary>차트(판정 박·키·수)는 R17 승인 때와 같다 (줄바꿈 무시 MD5).</summary>
        [Test]
        public void R18_Chart_Unchanged()
        {
            var ta = Resources.Load<TextAsset>("Charts/stage1");
            Assert.IsNotNull(ta);
            var bytes = Encoding.UTF8.GetBytes(ta.text.Replace("\r", ""));
            using var md5 = MD5.Create();
            var hash = System.BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual("f616f75e9e0ccc7845f9f99f5e630eae", hash, "stage1.json이 R17 승인 때와 다름 (판정이 바뀜)");
        }

        /// <summary>돌진 적: 서 있음 → 걷기 → 부드럽게 빨라짐 → 판정 2박 전부터 질주. 판정 순간 판정 자리.</summary>
        [Test]
        public void R18_ChargeSpeed_WalkRampSprint()
        {
            Assert.AreEqual(0f, EventProp.ChargeAhead(0f), 1e-4f);
            Assert.AreEqual(0f, EventProp.ChargeSpeed(GameConfig.Map.ChargeStartBeats + 1f));
            Assert.AreEqual(GameConfig.Map.ChargeWalkSpeed, EventProp.ChargeSpeed(GameConfig.Map.ChargeStartBeats - 0.5f), 1e-4f);
            Assert.AreEqual(GameConfig.Map.ChargeSprintSpeed, EventProp.ChargeSpeed(1f), 1e-4f);
            Assert.Less(GameConfig.Map.ChargeWalkSpeed, 1.6f, "걷기는 달리기보다 확실히 느림 (m/박)");
            float prev = EventProp.ChargeSpeed(0.01f);
            for (float r = 0.05f; r < 14f; r += 0.05f)
            {
                float s = EventProp.ChargeSpeed(r);
                Assert.Less(Mathf.Abs(s - prev), GameConfig.Map.ChargeWalkSpeed + 0.01f, $"{r:0.00}박 전 속도가 갑자기 튀지 않음");
                Assert.LessOrEqual(s, prev + 1e-4f, "판정에 가까울수록 빨라짐");
                prev = s;
            }
            // 앞 거리는 속도의 적분 (늘어나기만, 서 있는 동안은 그대로)
            Assert.AreEqual(EventProp.ChargeAhead(GameConfig.Map.ChargeStartBeats), EventProp.ChargeAhead(GameConfig.Map.ChargeStartBeats + 5f), 1e-3f);
            Assert.Greater(EventProp.ChargeAhead(GameConfig.Map.ChargeStartBeats), 15f, "멀리서부터 보임");
        }
    }
}
