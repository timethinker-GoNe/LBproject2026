using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    public class GridManager : MonoBehaviour
    {
        public int chunkSize = 9;
        public float gridSize = 1f;
        public KeyCode generateGridKey = KeyCode.G;

        private List<Vector3> gridList = new();
        public Color gridColor = new Color(0, 1, 0, 0.25f); // 연녹색 반투명

        private void GenerateChunks_PlayerPos()
        {
            var player = PlayerCharacter.GetFirst();
            Vector3 playerPos = player == null ? Vector3.zero : player.transform.position;

            float chunkWorldSize = chunkSize * gridSize;
            int centerX = Mathf.FloorToInt(playerPos.x / chunkWorldSize);
            int centerY = Mathf.FloorToInt(playerPos.z / chunkWorldSize);

            gridList = new();

            // chunk x 9
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int chunkX = centerX + dx;
                    int chunkY = centerY + dy;

                    Vector3 chunkCoord = new Vector3(chunkX, 0, chunkY);
                    GenerateChunk(chunkCoord);
                }
            }
        }

        public void GenerateChunk(Vector3 chunkCoord)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Vector3 worldPos = new Vector3(
                        (chunkCoord.x * chunkSize + x) * gridSize + (gridSize / 2),
                        0,
                        (chunkCoord.z * chunkSize + y) * gridSize + (gridSize / 2)
                    );
                    gridList.Add(worldPos);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (gridList == null)
                return;

            Gizmos.color = gridColor;
            Vector3 size = new Vector3(gridSize, 0.1f, gridSize); // 한 칸 크기만큼만 그리기

            foreach (var origin in gridList)
            {
                Gizmos.DrawCube(origin + (size / 2f), size); // 셀 중심 위치에 그리기
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(generateGridKey))
            {
                gridList.Clear();
                GenerateChunks_PlayerPos();
            }
        }
    }
}
