using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game
{
    public class ArrowDrawer : MonoBehaviour
    {
        public Color arrowColA;
        public Color arrowColB;
        public float lineWidth;
        public float headSize;
        public Material material;
        private List<Transform> activeArrows;
        private BoardUI boardUI;
        private Camera cam;
        private bool isDrawing;
        private Coord startCoord;

        private void Start()
        {
            activeArrows = new List<Transform>();
            boardUI = FindObjectOfType<BoardUI>();
            cam = Camera.main;
        }

        private void Update()
        {
            var mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(1)) isDrawing = boardUI.TryGetSquareUnderMouse(mousePos, out startCoord);

            if (isDrawing && Input.GetMouseButtonUp(1))
            {
                Coord endCoord;
                if (boardUI.TryGetSquareUnderMouse(mousePos, out endCoord))
                {
                    isDrawing = false;
                    var col = Input.GetKey(KeyCode.LeftShift) ? arrowColB : arrowColA;
                    CreateArrow(boardUI.PositionFromCoord(startCoord), boardUI.PositionFromCoord(endCoord), col);
                }
            }

            if (Input.GetMouseButtonDown(0)) ClearArrows();
        }

        private void ClearArrows()
        {
            for (var i = activeArrows.Count - 1; i >= 0; i--) Destroy(activeArrows[i].gameObject);
            activeArrows.Clear();
        }

        private void CreateArrow(Vector2 startPos, Vector2 endPos, Color col)
        {
            var meshHolder = new GameObject("Arrow");
            meshHolder.layer = LayerMask.NameToLayer("Arrows");
            meshHolder.transform.parent = transform;
            var renderer = meshHolder.AddComponent<MeshRenderer>();
            var filter = meshHolder.AddComponent<MeshFilter>();
            renderer.material = material;
            renderer.material.color = col;
            meshHolder.transform.position = new Vector3(0, 0, -1 - activeArrows.Count * 0.1f);

            var mesh = new Mesh();
            filter.mesh = mesh;

            ArrowMesh.CreateArrowMesh(ref mesh, startPos, endPos, lineWidth, headSize);
            activeArrows.Add(meshHolder.transform);
        }
    }
}