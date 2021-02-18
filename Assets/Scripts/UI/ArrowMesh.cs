using UnityEngine;

public static class ArrowMesh
{
    public static void CreateArrowMesh(ref Mesh mesh, Vector2 start, Vector2 end, float lineWidth, float headSize,
        bool flatHead = true)
    {
        if (mesh == null) mesh = new Mesh();
        var forward = (end - start).normalized;
        Vector2 perp = Vector3.Cross(forward, Vector3.forward);

        var verts = new Vector3[7];

        var actualHeadSize = lineWidth * 2 + headSize;
        var headBackAmount = flatHead ? 0 : 0.35f;
        end -= forward * actualHeadSize;
        verts[0] = start - perp * lineWidth / 2;
        verts[1] = start + perp * lineWidth / 2;
        verts[2] = end - perp * lineWidth / 2;
        verts[3] = end + perp * lineWidth / 2;
        verts[4] = end + forward * actualHeadSize;
        verts[5] = end - forward * actualHeadSize * headBackAmount - perp * actualHeadSize / 2;
        verts[6] = end - forward * actualHeadSize * headBackAmount + perp * actualHeadSize / 2;

        mesh.vertices = verts;
        mesh.triangles = new[] {0, 2, 1, 1, 2, 3, 2, 5, 4, 2, 4, 3, 3, 4, 6};
        mesh.RecalculateBounds();
    }
}