using UnityEngine;

namespace FarmingEngine
{
    /// <summary>씬 Inspector에 배치 안내 메모를 표시하는 더미 컴포넌트</summary>
    [AddComponentMenu("")]
    public class SceneNote : MonoBehaviour
    {
        [TextArea(5, 12)]
        public string note = "배치 안내를 여기에 입력하세요.";
    }
}
