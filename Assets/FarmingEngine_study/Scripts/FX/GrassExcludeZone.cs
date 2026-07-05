using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 이 컴포넌트가 붙은 오브젝트의 XZ 영역(Renderer.bounds)에는 GPU 잔디(GpuGrassTool)가 생성되지 않는다.
    /// 경작 가능한 땅(흙 Plane) 등, GrassGroundZone 위에 겹쳐 놓이는 구역에 붙인다.
    /// </summary>
    public class GrassExcludeZone : MonoBehaviour
    {
    }
}
