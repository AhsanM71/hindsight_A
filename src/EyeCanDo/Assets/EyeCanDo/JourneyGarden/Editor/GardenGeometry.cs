using System;
using System.Collections.Generic;
using UnityEngine;

namespace EyeCanDo.Editor
{
    // Small original mesh library: rounded architecture, equipment, and pictorial symbols.
    internal static class GardenGeometry
    {
        public static Mesh RoundedBox(Vector3 size, float radius)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
            Vector3 half = size * .5f, inner = half - Vector3.one * radius;
            Vector3[] normals = {Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back};
            const int steps = 8;
            foreach (var normal in normals)
            {
                Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 w = Vector3.Cross(normal, u);
                int start = v.Count;
                for (int y=0;y<=steps;y++) for(int x=0;x<=steps;x++)
                {
                    Vector3 p = Vector3.Scale(normal + u * (2f*x/steps-1) + w * (2f*y/steps-1),half);
                    Vector3 q = new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x), Mathf.Clamp(p.y,-inner.y,inner.y), Mathf.Clamp(p.z,-inner.z,inner.z));
                    Vector3 d = (p-q).normalized; v.Add(q+d*radius); n.Add(d);
                    if(x==steps || y==steps)continue;
                    int a=start+y*(steps+1)+x; tri.AddRange(new[]{a,a+1,a+steps+1,a+1,a+steps+2,a+steps+1});
                }
            }
            var mesh = new Mesh {name="Rounded box"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Lathe(Vector2[] profile, int segments=48)
        {
            var v=new List<Vector3>();var tri=new List<int>();
            for(int y=0;y<profile.Length;y++)for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;v.Add(new Vector3(Mathf.Cos(a)*profile[y].x,profile[y].y,Mathf.Sin(a)*profile[y].x));
                if(y==profile.Length-1||i==segments)continue;
                int k=y*(segments+1)+i;tri.AddRange(new[]{k,k+segments+1,k+1,k+1,k+segments+1,k+segments+2});
            }
            var m=new Mesh {name="Rounded lathe"};m.SetVertices(v);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static Mesh Shape(Vector2[] points, float depth=.12f)
        {
            var v=new List<Vector3>();var tri=new List<int>();
            // Pictograms here are star-convex, allowing a triangle fan.
            Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
            v.Add(new Vector3(center.x,center.y,-depth*.5f));v.Add(new Vector3(center.x,center.y,depth*.5f));
            for(int i=0;i<points.Length;i++){v.Add(new Vector3(points[i].x,points[i].y,-depth*.5f));v.Add(new Vector3(points[i].x,points[i].y,depth*.5f));}
            for(int i=0;i<points.Length;i++)
            {
                int a=2+i*2,b=2+((i+1)%points.Length)*2;
                tri.AddRange(new[]{0,b,a,1,a+1,b+1,a,b,b+1,a,b+1,a+1});
            }
            var m=new Mesh {name="Extruded picture symbol"};m.SetVertices(v);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static Mesh Star()
        {
            var p=new Vector2[10];for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;p[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.5f:.24f);}return Shape(p);
        }
        public static Mesh Drop()
        {
            var p=new List<Vector2>{new Vector2(0,.65f)};
            for(int i=0;i<=24;i++){float a=Mathf.PI*.85f+i*(Mathf.PI*1.3f)/24;p.Add(new Vector2(Mathf.Cos(a)*.37f,Mathf.Sin(a)*.39f-.1f));}
            return Shape(p.ToArray(),.15f);
        }
        public static Mesh Roof(float width,float height,float depth)
        {
            // Long, softened gable silhouette. The roof trim uses a second, larger copy.
            return Shape(new[]{new Vector2(-width*.5f,0),new Vector2(width*.5f,0),new Vector2(width*.48f,height*.18f),new Vector2(.25f,height),new Vector2(-.25f,height),new Vector2(-width*.48f,height*.18f)},depth);
        }
        public static Mesh Island(float rx,float rz,float top,float bottom)
        {
            var v=new List<Vector3>();var tri=new List<int>();const int count=120;
            v.Add(new Vector3(0,top,0));
            for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++)
            {
                float a=i*2*Mathf.PI/count;float wave=1+.025f*Mathf.Sin(a*5)+.015f*Mathf.Cos(a*9);
                float s=ring==0?.92f:ring==1?1:1.01f;float y=ring==0?top:ring==1?top-.25f:bottom;
                v.Add(new Vector3(rx*Mathf.Cos(a)*s*wave,y,rz*Mathf.Sin(a)*s*wave));
            }
            for(int i=0;i<count;i++)
            {
                int j=(i+1)%count;tri.AddRange(new[]{0,1+j,1+i});
                for(int ring=0;ring<2;ring++){int a=1+ring*count+i,b=1+ring*count+j;tri.AddRange(new[]{a,b,a+count,b,b+count,a+count});}
            }
            var m=new Mesh{name="Soft scalloped garden terrain"};m.SetVertices(v);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
