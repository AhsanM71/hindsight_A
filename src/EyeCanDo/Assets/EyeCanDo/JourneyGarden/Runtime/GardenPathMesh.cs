using System.Collections.Generic;
using UnityEngine;

namespace EyeCanDo
{
    public static class GardenPathMesh
    {
        public static Mesh Create(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float width, int steps = 48)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps, u = 1 - t;
                Vector3 p = u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d;
                Vector3 tangent = 3*u*u*(b-a) + 6*u*t*(c-b) + 3*t*t*(d-c);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized * width * .5f;
                vertices.Add(p - side); vertices.Add(p + side); uv.Add(new Vector2(0,t)); uv.Add(new Vector2(1,t));
                if (i == steps) continue;
                int k = i*2; triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});
            }
            var mesh = new Mesh { name = "Garden curved ribbon" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetUVs(0,uv); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
