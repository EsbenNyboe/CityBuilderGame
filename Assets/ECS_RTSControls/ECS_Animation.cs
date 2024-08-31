/*
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using UnityEngine;

namespace ECS_AnimationSystem
{
    public static class ECS_Animation
    {
        public static Mesh CreateMesh(float meshWidth, float meshHeight)
        {
            var vertices = new Vector3[4];
            var uv = new Vector2[4];
            var triangles = new int[6];

            var meshWidthHalf  = meshWidth  / 2f;
            var meshHeightHalf = meshHeight / 2f;

            vertices[0] = new Vector3(-meshWidthHalf,  meshHeightHalf);
            vertices[1] = new Vector3( meshWidthHalf,  meshHeightHalf);
            vertices[2] = new Vector3(-meshWidthHalf, -meshHeightHalf);
            vertices[3] = new Vector3( meshWidthHalf, -meshHeightHalf);

            uv[0] = new Vector2(0, 1);
            uv[1] = new Vector2(1, 1);
            uv[2] = new Vector2(0, 0);
            uv[3] = new Vector2(1, 0);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 1;
            triangles[5] = 3;

            var mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            return mesh;
        }
    }
}