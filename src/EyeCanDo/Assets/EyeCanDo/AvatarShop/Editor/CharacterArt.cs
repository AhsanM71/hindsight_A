using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EyeCanDo.Editor
{
    // Original, parameterized mesh artwork. No external models or textures.
    public static class CharacterArt
    {
        public const string Root="Assets/EyeCanDo/AvatarShop";
        static Mesh sphere;
        public static Color C(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
        public static Material Mat(string hex)
        {
            string path=Root+"/Materials/"+hex+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",C(hex));m.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(m,path);}return m;
        }
        static Mesh Save(Mesh m,string name)
        {
            string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old!=null){Object.DestroyImmediate(m);return old;}m.name=name;AssetDatabase.CreateAsset(m,path);return m;
        }
        public static GameObject Group(string name,Transform parent=null,Vector3 position=default)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
        public static GameObject MeshObject(string name,Transform parent,Mesh mesh,Vector3 p,Vector3 scale,string color,AvatarColorChannel? tint=null)
        {
            var g=Group(name,parent,p);g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
            if(tint.HasValue)g.AddComponent<AvatarTint>().channel=tint.Value;return g;
        }
        public static Mesh Sphere()
        {
            if(sphere!=null)return sphere;var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();const int rings=20,segments=28;
            for(int r=0;r<=rings;r++)for(int s=0;s<=segments;s++){float a=Mathf.PI*r/rings,b=Mathf.PI*2*s/segments;var n=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));vertices.Add(n*.5f);normals.Add(n);}
            for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){int a=r*(segments+1)+s,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var m=new Mesh();m.SetVertices(vertices);m.SetNormals(normals);m.SetTriangles(triangles,0);m.RecalculateBounds();sphere=Save(m,"Rounded form");return sphere;
        }
        public static GameObject Ball(string name,Transform parent,Vector3 p,Vector3 scale,string color,AvatarColorChannel? tint=null)=>MeshObject(name,parent,Sphere(),p,scale,color,tint);
        public static GameObject Tube(string name,Transform parent,Vector3[] path,float radius,string color)
        {
            var v=new List<Vector3>();var tr=new List<int>();const int sides=10;
            for(int j=0;j<path.Length;j++)
            {
                var tangent=(path[Mathf.Min(j+1,path.Length-1)]-path[Mathf.Max(0,j-1)]).normalized;var side=Vector3.Cross(tangent,Vector3.forward).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;var normal=Vector3.Cross(tangent,side).normalized;
                for(int i=0;i<sides;i++){float a=2*Mathf.PI*i/sides;v.Add(path[j]+radius*(side*Mathf.Cos(a)+normal*Mathf.Sin(a)));}
            }
            for(int j=0;j<path.Length-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;tr.AddRange(new[]{a,b,a+sides,b,b+sides,a+sides});}
            var m=new Mesh();m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return MeshObject(name,parent,Save(m,name),Vector3.zero,Vector3.one,color);
        }
        static Vector3[] Arc(Vector3 center,float x,float y,float from,float to,int n=24)
        {var points=new Vector3[n+1];for(int i=0;i<=n;i++){float a=Mathf.Lerp(from,to,i/(float)n)*Mathf.Deg2Rad;points[i]=center+new Vector3(Mathf.Cos(a)*x,Mathf.Sin(a)*y,0);}return points;}
        static GameObject Prefab(GameObject g,string name)
        {var p=PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(g);return p;}
        public static GameObject Face(int index)
        {
            var g=Group("Face "+index);var t=g.transform;float w=new[]{1.42f,1.29f,1.52f,1.38f}[index],h=new[]{1.38f,1.50f,1.32f,1.43f}[index];
            Ball("Soft face",t,Vector3.zero,new(w,h,1.18f),"A66C48",AvatarColorChannel.Skin);
            foreach(int sign in new[]{-1,1})
            {
                Ball("Ear",t,new(sign*w*.48f,-.04f,.01f),new(.27f,.40f,.26f),"A66C48",AvatarColorChannel.Skin);
                Ball("Ear inner",t,new(sign*w*.50f,-.05f,-.13f),new(.12f,.22f,.055f),"C68A68");
                var eye=Group("Blink eye",t,new(sign*.29f,.08f,-.53f)).transform;
                Ball("Eye white",eye,Vector3.zero,new(.37f,.45f,.14f),"FFFDF1");
                Ball("Iris",eye,new(0,0,-.079f),new(.23f,.30f,.08f),"49392F");
                Ball("Pupil",eye,new(0,0,-.12f),new(.13f,.20f,.035f),"182C36");
                Ball("Catchlight",eye,new(-.04f,.065f,-.145f),new(.07f,.08f,.025f),"FFFFFF");
                Tube("Brow "+index+" "+sign,t,Arc(new(sign*.29f,.30f,-.55f),.19f,.10f,25,155,12),.038f,"3D2C2C");
                Ball("Warm cheek",t,new(sign*.43f,-.21f,-.50f),new(.25f,.12f,.035f),"D58B71");
                if(index==2)for(int f=0;f<3;f++)Ball("Freckle",t,new(sign*(.33f+f*.065f),-.17f-(f%2)*.04f,-.55f),new(.025f,.025f,.02f),"78492F");
            }
            Ball("Button nose",t,new(0,-.10f,-.61f),new(.22f,.23f,.22f),"A66C48",AvatarColorChannel.Skin);
            Tube("Smile "+index,t,Arc(new(0,-.26f,-.565f),index==3?.24f:.20f,.115f,198,342),.027f,"673C35");
            if(index==1)Ball("Happy tooth",t,new(0,-.32f,-.595f),new(.19f,.065f,.035f),"FFFDF1");
            return Prefab(g,"Face"+index);
        }
        public static GameObject Hair(int index)
        {
            var g=Group("Hair "+index);var t=g.transform;var crown=Group("Crown",t).transform;var sides=Group("Sides",t).transform;
            if(index!=4)
            {
                // Hat replaces only the crown; side hair stays below the brim.
                Ball("Hair crown",crown,new(0,.49f,.06f),new(1.47f,.65f,1.20f),"322725",AvatarColorChannel.Hair);
                if(index==0||index==1)
                {
                    for(int i=0;i<5;i++){var b=Ball("Soft fringe",crown,new(-.48f+i*.24f,.46f+Mathf.Sin(i)*.05f,-.48f),new(.40f,.35f,.30f),"322725",AvatarColorChannel.Hair);b.transform.localRotation=Quaternion.Euler(0,0,-20);}
                    foreach(int sign in new[]{-1,1})Ball("Side hair",sides,new(sign*.64f,index==1?-.28f:.05f,.13f),index==1?new(.28f,1.08f,1.0f):new Vector3(.18f,.42f,.60f),"322725",AvatarColorChannel.Hair);
                    if(index==1)Ball("Bob back",sides,new(0,-.24f,.44f),new(1.38f,1.0f,.47f),"322725",AvatarColorChannel.Hair);
                }
                if(index==2)
                {
                    for(int ring=0;ring<3;ring++)for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;float r=.57f-ring*.15f;Ball("Crown curl",crown,new(Mathf.Cos(a)*r,.48f+ring*.14f,Mathf.Sin(a)*r),new(.36f,.35f,.36f),"322725",AvatarColorChannel.Hair);}
                    foreach(int sign in new[]{-1,1})for(int i=0;i<3;i++)Ball("Side curl",sides,new(sign*.64f,.10f-i*.16f,.09f+i*.12f),new(.25f,.28f,.34f),"322725",AvatarColorChannel.Hair);
                }
                if(index==3)
                {
                    foreach(int sign in new[]{-1,1})
                    {
                        Ball("Puff",sides,new(sign*.91f,-.12f,.24f),new(.65f,.65f,.65f),"322725",AvatarColorChannel.Hair);
                        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Ball("Puff curl",sides,new(sign*.91f+Mathf.Cos(a)*.21f,-.12f+Mathf.Sin(a)*.21f,.05f),new(.29f,.28f,.28f),"322725",AvatarColorChannel.Hair);}
                        Ball("Puff ribbon",sides,new(sign*.68f,-.10f,-.07f),new(.14f,.24f,.18f),"E9B650");
                    }
                }
            }
            return Prefab(g,"Hair"+index);
        }
        public static GameObject Hat(bool star)
        {
            var g=Group(star?"Star hat":"Colourful cap");var t=g.transform;
            Ball("Hat crown",t,new(0,.52f,.06f),new(1.57f,.64f,1.34f),star?"7655A1":"EFAC4B");
            Ball("Hat band",t,new(0,.37f,0),new(1.59f,.17f,1.37f),star?"B799DD":"2C9C9B");
            if(!star){Ball("Cap visor",t,new(0,.34f,-.64f),new(1.15f,.13f,.92f),"2C9C9B");Ball("Top button",t,new(0,.87f,.05f),new(.16f,.13f,.16f),"2C9C9B");}
            else
            {
                Star(t,new(0,.66f,-.64f),.28f,"Star badge");
                Ball("Top bobble",t,new(0,1.0f,.04f),new(.27f,.28f,.27f),"F7CE69");
            }
            return Prefab(g,star?"StarHat":"Cap");
        }
        public static void Star(Transform t,Vector3 p,float size,string name)
        {
            var vertices=new List<Vector3>{new(0,0,-.07f),new(0,0,.07f)};for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;float r=i%2==0?size:size*.46f;vertices.Add(new(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));}
            var tr=new List<int>();for(int i=0;i<10;i++){int a=i+2,b=(i+1)%10+2;tr.AddRange(new[]{0,b,a,1,a,b});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();var g=MeshObject(name,t,Save(mesh,name+size),p,Vector3.one,"F7CE69");
        }
        public static GameObject Glasses()
        {
            var g=Group("Fun glasses");var t=g.transform;
            foreach(int side in new[]{-1,1})
            {
                Tube("Glasses rim "+side,t,Arc(new(side*.29f,.08f,-.66f),.25f,.28f,0,360,36),.043f,"387BAB");
                Tube("Glasses arm "+side,t,new[]{new Vector3(side*.52f,.12f,-.62f),new Vector3(side*.68f,.15f,-.4f),new Vector3(side*.7f,.10f,.05f)},.032f,"387BAB");
            }
            Tube("Glasses bridge",t,new[]{new Vector3(-.06f,.10f,-.68f),new Vector3(0,.14f,-.70f),new Vector3(.06f,.10f,-.68f)},.032f,"387BAB");
            return Prefab(g,"Glasses");
        }
        public static GameObject Backpack()
        {
            var g=Group("Small backpack");var t=g.transform;
            Ball("Backpack body",t,new(0,0,.29f),new(1.05f,1.23f,.57f),"D97B55");
            Ball("Pocket",t,new(0,-.20f,.57f),new(.73f,.55f,.19f),"EFAF72");
            Tube("Pocket zipper",t,new[]{new Vector3(-.28f,-.1f,.68f),new Vector3(.28f,-.1f,.68f)},.023f,"FFF3D7");
            Tube("Carry loop",t,Arc(new(0,.57f,.31f),.17f,.18f,0,180),.055f,"277F86");
            foreach(int side in new[]{-1,1})Tube("Backpack strap "+side,t,new[]{new Vector3(side*.31f,.40f,.1f),new Vector3(side*.39f,.52f,-.20f),new Vector3(side*.43f,.18f,-.63f),new Vector3(side*.42f,-.40f,-.62f),new Vector3(side*.32f,-.49f,.10f)},.055f,"277F86");
            Star(t,new(0,.21f,.60f),.16f,"Backpack star");return Prefab(g,"Backpack");
        }
        public static GameObject Avatar(CosmeticCatalog catalog)
        {
            var g=Group("Storybook child");var view=g.AddComponent<AvatarView>();view.catalog=catalog;
            var body=Group("Animated body",g.transform).transform;view.body=body;
            Ball("Tee",body,new(0,2.70f,0),new(1.11f,1.42f,.67f),"269F9C",AvatarColorChannel.Shirt);
            Ball("Tee hem",body,new(0,2.12f,0),new(1.12f,.24f,.68f),"269F9C",AvatarColorChannel.Shirt);
            Ball("Neck",body,new(0,3.36f,0),new(.42f,.46f,.43f),"A66C48",AvatarColorChannel.Skin);
            Ball("Collar",body,new(0,3.29f,-.05f),new(.56f,.15f,.48f),"F7E9CF");
            // EyeCanDo eye emblem sits on the shirt, following its animation.
            Ball("EyeCanDo eye",body,new(0,2.82f,-.342f),new(.55f,.29f,.045f),"FFF9E5");
            Ball("Emblem iris",body,new(0,2.82f,-.37f),new(.22f,.23f,.04f),"225764");
            Ball("Emblem sparkle",body,new(-.035f,2.87f,-.395f),new(.065f,.065f,.02f),"FFFFFF");
            Ball("Shorts waist",body,new(0,1.94f,.01f),new(1.06f,.44f,.64f),"344F73",AvatarColorChannel.Shorts);
            foreach(int side in new[]{-1,1})
            {
                Ball("Shorts leg",body,new(side*.28f,1.66f,0),new(.53f,.67f,.62f),"344F73",AvatarColorChannel.Shorts);
                Ball("Leg",body,new(side*.29f,.98f,0),new(.33f,1.12f,.37f),"A66C48",AvatarColorChannel.Skin);
                Ball("Sock",body,new(side*.29f,.43f,-.025f),new(.35f,.32f,.38f),"FFF6E7");
                Ball("Sneaker sole",body,new(side*.29f,.13f,-.16f),new(.49f,.22f,.82f),"F3E7D1");
                Ball("Sneaker",body,new(side*.29f,.26f,-.14f),new(.47f,.32f,.76f),"E7A84C");
                for(int i=0;i<3;i++)Ball("Lace",body,new(side*.29f,.39f-i*.025f,-.19f-i*.095f),new(.26f,.035f,.04f),"FFF9E5");
                var arm=Group(side>0?"Right shoulder":"Left shoulder",body,new(side*.57f,3.12f,0)).transform;
                Ball("Sleeve",arm,new(0,-.17f,0),new(.44f,.54f,.53f),"269F9C",AvatarColorChannel.Shirt);
                Ball("Forearm",arm,new(0,-.70f,0),new(.25f,.85f,.29f),"A66C48",AvatarColorChannel.Skin);
                var hand=Group(side>0?"Right hand attachment":"Left hand attachment",arm,new(0,-1.10f,0)).transform;
                Ball("Hand",hand,Vector3.zero,new(.28f,.34f,.26f),"A66C48",AvatarColorChannel.Skin);
                Ball("Thumb",hand,new(-side*.12f,.04f,-.02f),new(.14f,.20f,.15f),"A66C48",AvatarColorChannel.Skin);
                if(side>0){view.rightArm=arm;view.rightHandAttachment=hand;}else{view.leftArm=arm;view.leftHandAttachment=hand;}
                arm.localRotation=Quaternion.Euler(0,0,side*8);
            }
            view.headAttachment=Group("Head attachment",body,new(0,4.06f,0)).transform;
            view.faceAttachment=Group("Face attachment",view.headAttachment).transform;view.hairAttachment=Group("Hair attachment",view.headAttachment).transform;
            view.hatAttachment=Group("Hat attachment",view.headAttachment).transform;view.glassesAttachment=Group("Glasses attachment",view.headAttachment).transform;
            view.backAttachment=Group("Back attachment",body,new(0,2.70f,.25f)).transform;
            // Keep the prefab structural. Runtime applies either committed or temporary appearance.
            return Prefab(g,"StorybookChild");
        }
    }
}
