using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EyeCanDo.Editor
{
    // Original artwork, authored from geometric shapes. No external art service is used.
    public static class MvpArt
    {
        public const string Root = "Assets/EyeCanDo";
        public static readonly Color Ink = Hex("234B55");
        public static readonly Color Green = Hex("33845F");
        public static readonly Color Cream = Hex("FFF8E9");
        public static readonly Color Teal = Hex("63CEC2");
        public static readonly Color Purple = Hex("B5A1E4");
        public static readonly Color Gold = Hex("F4C66B");
        static readonly Dictionary<string, Material> materials = new();
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        public static void Folder(string path) => Directory.CreateDirectory(path);
        public static Material Mat(string name, Color color)
        {
            if (materials.TryGetValue(name, out var found)) return found;
            string path = $"{Root}/Art/Materials/{name}.mat";
            found = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (found == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is required.");
                found = new Material(shader);
                AssetDatabase.CreateAsset(found, path);
            }
            found.SetColor("_BaseColor", color);
            found.SetFloat("_Smoothness", .22f);
            materials[name] = found;
            return found;
        }
        public static GameObject Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
        public static GameObject Shape(string name, PrimitiveType kind, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(kind);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(ColorUtility.ToHtmlStringRGB(color), color);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        public static GameObject Ball(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
            => Shape(name, PrimitiveType.Sphere, parent, position, scale, color);
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
            => Shape(name, PrimitiveType.Cube, parent, position, scale, color);
        public static GameObject Pill(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
            => Shape(name, PrimitiveType.Capsule, parent, position, scale, color);
        public static GameObject Disc(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
            => Shape(name, PrimitiveType.Cylinder, parent, position, scale, color);

        public static GameObject Owl()
        {
            var root = Group("Owl");
            var t = root.transform;
            Color plum = Hex("8B7AC0"), lilac = Hex("B9A5E1");
            Ball("Body", t, new(0, 1.6f, 0), new(2.7f, 3, 2), lilac);
            Ball("Cream bib", t, new(0, 1.35f, -.82f), new(1.75f, 2.02f, .6f), Cream);
            Ball("Head", t, new(0, 3.05f, -.04f), new(3.05f, 2.45f, 2.1f), lilac);
            Pill("Left feather", t, new(-.96f, 3.94f, .06f), new(.68f, .65f, .66f), plum).transform.localRotation = Quaternion.Euler(0, 0, -24);
            Pill("Right feather", t, new(.96f, 3.94f, .06f), new(.68f, .65f, .66f), plum).transform.localRotation = Quaternion.Euler(0, 0, 24);
            var eyes = new List<Transform>();
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * .66f;
                Ball("Face", t, new(x, 3.12f, -.94f), new(1.42f, 1.58f, .44f), Cream);
                var eye = Group("Eye", t).transform; eye.localPosition = new(x, 3.15f, -1.16f);
                Ball("Iris", eye, Vector3.zero, new(.62f, .76f, .24f), Ink);
                Ball("Sparkle", eye, new(-.1f, .16f, -.12f), new(.18f, .21f, .08f), Color.white);
                eyes.Add(eye);
                Ball("Cheek", t, new(side * 1.03f, 2.68f, -1.06f), new(.42f, .22f, .12f), Hex("F3B8AF"));
                var wing = Ball("Wing", t, new(side * 1.36f, 1.84f, -.04f), new(.65f, 1.88f, .9f), plum);
                wing.transform.localRotation = Quaternion.Euler(0, 0, side * -22);
                Ball("Foot", t, new(side * .59f, .19f, -.37f), new(.77f, .29f, 1.12f), Gold);
            }
            Ball("Beak", t, new(0, 2.75f, -1.24f), new(.38f, .46f, .37f), Gold);
            // A small clinic scarf, with no alarming medical imagery.
            Ball("Scarf", t, new(0, 2.23f, -.04f), new(2.28f, .28f, 1.8f), Teal);
            Box("Scarf tail", t, new(.64f, 1.86f, -1.03f), new(.34f, .74f, .15f), Teal).transform.localRotation = Quaternion.Euler(0, 0, -15);
            root.AddComponent<GentleIdle>();
            root.AddComponent<OwlBlink>().eyes = eyes.ToArray();
            return root;
        }

        public static GameObject Equipment(int kind)
        {
            var root = Group(kind == 0 ? "Eye-drop bottle" : kind == 1 ? "Slit-lamp microscope" : "Picture vision chart");
            var t = root.transform;
            if (kind == 0)
            {
                Pill("Bottle", t, new(0, 1.45f, 0), new(1.65f, 1.25f, 1.45f), Cream);
                Disc("Shoulder", t, new(0, 2.35f, 0), new(1.24f, .2f, 1.13f), Teal);
                Disc("Nozzle", t, new(0, 2.75f, 0), new(.54f, .32f, .54f), Cream);
                Pill("Cap", t, new(0, 3.26f, 0), new(.8f, .38f, .8f), Teal);
                Ball("Label", t, new(0, 1.5f, -.71f), new(1.17f, 1.1f, .14f), Teal);
                Ball("Drop", t, new(0, 1.46f, -.81f), new(.43f, .56f, .1f), Cream);
                Ball("Drop tip", t, new(0, 1.7f, -.81f), new(.22f, .32f, .1f), Cream);
                // Floating water droplet beside the bottle.
                Ball("Floating droplet", t, new(1.15f, 2.6f, 0), new(.4f, .57f, .4f), Teal);
            }
            else if (kind == 1)
            {
                Box("Table", t, new(0, .8f, 0), new(3.1f, .2f, 1.65f), Cream);
                Disc("Table pedestal", t, new(0, .4f, 0), new(.5f, .4f, .5f), Ink);
                Ball("Base", t, new(0, .12f, 0), new(2.8f, .25f, 1.7f), Purple);
                Disc("Optics column", t, new(-.55f, 1.55f, .18f), new(.3f, .66f, .3f), Purple);
                Box("Optics arm", t, new(-.25f, 2.35f, .1f), new(1.1f, .3f, .38f), Purple);
                Box("Microscope head", t, new(-.42f, 2.65f, -.08f), new(1.15f, .7f, .62f), Cream);
                foreach (float x in new[] { -.73f, -.19f })
                {
                    var tube = Disc("Eyepiece tube", t, new(x, 2.72f, -.55f), new(.34f, .28f, .34f), Ink);
                    tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Ball("Lens", t, new(x, 2.72f, -.84f), new(.27f, .27f, .08f), Teal);
                }
                Pill("Lamp tower", t, new(.53f, 2.5f, .36f), new(.3f, .8f, .3f), Gold);
                foreach (float x in new[] { .92f, 1.43f })
                    Disc("Patient support", t, new(x, 1.75f, -.05f), new(.09f, .91f, .09f), Ink);
                Ball("Chin rest", t, new(1.18f, 1.6f, -.09f), new(.77f, .2f, .48f), Purple);
                Box("Forehead rest", t, new(1.18f, 2.64f, -.03f), new(.68f, .17f, .22f), Purple);
                Ball("Adjustment dial", t, new(-1.02f, 1.2f, -.65f), new(.3f, .3f, .3f), Gold);
            }
            else
            {
                Disc("Stand", t, new(0, .64f, .18f), new(.21f, .65f, .21f), Ink);
                Ball("Stand base", t, new(0, .1f, 0), new(2.3f, .2f, 1.4f), Gold);
                Box("Chart frame", t, new(0, 2.25f, .15f), new(2.65f, 2.85f, .22f), Gold);
                Box("Chart paper", t, new(0, 2.25f, -.01f), new(2.36f, 2.55f, .1f), Cream);
                Ball("Big circle", t, new(0, 3.01f, -.1f), new(.64f, .64f, .06f), Teal);
                foreach (float x in new[] { -.6f, .6f })
                {
                    Box("House", t, new(x, 2.22f, -.12f), new(.35f, .32f, .06f), Purple);
                    var roof = Box("Roof", t, new(x, 2.42f, -.1f), new(.33f, .33f, .06f), Purple);
                    roof.transform.localRotation = Quaternion.Euler(0, 0, 45);
                }
                foreach (float x in new[] { -.73f, 0, .73f })
                {
                    Ball("Picture dot", t, new(x, 1.55f, -.13f), new(.25f, .25f, .06f), Ink);
                    Box("Picture line", t, new(x, 1.25f, -.12f), new(.38f, .04f, .05f), Gold);
                }
            }
            return root;
        }

        public static void Garden(Transform root, float width = 28)
        {
            Box("Garden island", root, new(0, -.75f, 1), new(width, 1.2f, 15), Hex("A3D6AE"));
            Box("Lawn", root, new(0, -.12f, 1), new(width, .12f, 15), Hex("C4E6BF"));
            // Friendly little clinic at the back of the courtyard.
            Box("Clinic", root, new(0, 1.45f, 6), new(6.2f, 3.1f, 2), Cream);
            Ball("Roof", root, new(0, 3.06f, 6), new(7.2f, .7f, 2.7f), Teal);
            Pill("Door", root, new(0, 1.15f, 4.94f), new(1.25f, 1.1f, .1f), Hex("9BD6D6"));
            foreach (float x in new[] { -2f, 2f })
            {
                Box("Window frame", root, new(x, 1.75f, 4.96f), new(1.18f, 1.25f, .13f), Gold);
                Box("Window", root, new(x, 1.75f, 4.86f), new(.97f, 1.04f, .07f), Hex("A8DBE0"));
                Box("Window bar", root, new(x, 1.75f, 4.8f), new(.07f, 1.04f, .06f), Cream);
            }
            // Eye emblem above the door.
            Ball("Clinic eye", root, new(0, 2.74f, 4.86f), new(.9f, .46f, .1f), Cream);
            Ball("Clinic iris", root, new(0, 2.74f, 4.78f), new(.3f, .3f, .09f), Ink);
            for (int i = 0; i < 10; i++)
            {
                float x = i < 5 ? -width * .46f + (i % 2) * 1.2f : width * .46f - (i % 2) * 1.2f;
                float z = (i % 5) * 2.4f - 3;
                Disc("Tree trunk", root, new(x, .65f, z), new(.24f, .65f, .24f), Hex("C4A88A"));
                Ball("Round canopy", root, new(x, 1.9f, z), new(1.7f, 2.1f, 1.5f), i % 2 == 0 ? Hex("76BA99") : Hex("A3CC99"));
            }
            for (int i = 0; i < 16; i++)
            {
                float x = -11 + i * 1.45f;
                Ball("Garden pebble", root, new(x, -.02f, -4.3f + Mathf.Sin(i) * .4f), new(.5f, .15f, .4f), Cream);
                if (i % 3 != 0) continue;
                Disc("Flower stem", root, new(x, .23f, -3.8f), new(.055f, .25f, .055f), Green);
                for (int petal = 0; petal < 5; petal++)
                {
                    float a = petal * Mathf.PI * 2 / 5;
                    Ball("Flower petal", root, new(x + Mathf.Cos(a) * .15f, .49f + Mathf.Sin(a) * .15f, -3.8f), Vector3.one * .23f, i % 2 == 0 ? Gold : Purple);
                }
                Ball("Flower centre", root, new(x, .49f, -3.92f), Vector3.one * .15f, Cream);
            }
        }

        public static Sprite Icon(string name)
        {
            string path = $"{Root}/Art/Icons/{name}.png";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Color ink = Ink, cream = Cream;
            void Fill(Func<float, float, bool> mask, Color color)
            {
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                    if (mask((x + .5f) / size, (y + .5f) / size)) pixels[y * size + x] = color;
            }
            void Circle(float x, float y, float radius, Color color) => Fill((a,b) => (a-x)*(a-x)+(b-y)*(b-y) <= radius*radius, color);
            void Rect(float x, float y, float w, float h, Color color) => Fill((a,b) => Mathf.Abs(a-x)<w/2 && Mathf.Abs(b-y)<h/2,color);
            void Line(Vector2 start, Vector2 end, float radius, Color color)
            {
                Fill((x,y) => { var p = new Vector2(x,y); var v = end-start; return Vector2.Distance(p,start+v*Mathf.Clamp01(Vector2.Dot(p-start,v)/v.sqrMagnitude))<radius; }, color);
            }
            if (name == "Round")
                Fill((x,y) => { float a=Mathf.Max(Mathf.Abs(x-.5f)-.25f,0);float b=Mathf.Max(Mathf.Abs(y-.5f)-.25f,0);return a*a+b*b<.24f*.24f; },Color.white);
            else if (name == "Gear")
            {
                Circle(.5f,.5f,.29f,ink);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(new(.5f+Mathf.Cos(a)*.25f,.5f+Mathf.Sin(a)*.25f),new(.5f+Mathf.Cos(a)*.38f,.5f+Mathf.Sin(a)*.38f),.075f,ink);}
                Circle(.5f,.5f,.13f,cream);
            }
            else if (name == "Bag")
            {
                Line(new(.35f,.66f),new(.35f,.79f),.045f,ink);Line(new(.65f,.66f),new(.65f,.79f),.045f,ink);Line(new(.35f,.79f),new(.65f,.79f),.045f,ink);
                Rect(.5f,.43f,.65f,.54f,ink);Circle(.5f,.45f,.1f,Gold);
            }
            else if (name == "Back")
            { Line(new(.73f,.5f),new(.24f,.5f),.055f,ink);Line(new(.24f,.5f),new(.45f,.72f),.055f,ink);Line(new(.24f,.5f),new(.45f,.28f),.055f,ink); }
            else if (name == "Play")
                Fill((x,y)=>x>.3f&&x<.8f&&Mathf.Abs(y-.5f)<(.8f-x)*.75f,Color.white);
            else if (name == "Discover")
            { Circle(.43f,.59f,.27f,Teal);Circle(.43f,.59f,.18f,cream);Line(new(.61f,.39f),new(.81f,.18f),.07f,ink);Circle(.43f,.59f,.06f,Gold); }
            else if (name == "Watch")
            { Rect(.5f,.5f,.78f,.59f,Purple);Fill((x,y)=>x>.39f&&x<.7f&&Mathf.Abs(y-.5f)<(.7f-x)*.75f,cream); }
            else if (name == "Practice")
            {
                Line(new(.49f,.28f),new(.49f,.75f),.07f,ink);Line(new(.37f,.31f),new(.25f,.45f),.075f,ink);
                Rect(.55f,.31f,.36f,.3f,ink);Circle(.56f,.38f,.18f,ink);Circle(.49f,.82f,.045f,Gold);
                Line(new(.29f,.74f),new(.22f,.8f),.022f,Gold);Line(new(.68f,.75f),new(.75f,.83f),.022f,Gold);
            }
            else if (name == "Drop")
            { Rect(.5f,.39f,.45f,.48f,Teal);Circle(.5f,.62f,.225f,Teal);Rect(.5f,.77f,.23f,.2f,ink);Circle(.5f,.4f,.105f,cream);Fill((x,y)=>y>.4f&&y<.58f&&Mathf.Abs(x-.5f)<(.58f-y)*.55f,cream); }
            else if (name == "Lamp")
            {
                Rect(.5f,.2f,.78f,.1f,Purple);Rect(.34f,.47f,.08f,.52f,ink);Rect(.42f,.7f,.36f,.14f,Purple);Rect(.47f,.61f,.32f,.17f,ink);
                Circle(.39f,.61f,.07f,Teal);Circle(.56f,.61f,.07f,Teal);Rect(.76f,.48f,.04f,.55f,ink);Rect(.66f,.48f,.04f,.55f,ink);Rect(.71f,.75f,.16f,.07f,Purple);Rect(.71f,.4f,.18f,.07f,Purple);
            }
            else if (name == "Chart")
            { Rect(.5f,.51f,.63f,.77f,Gold);Rect(.5f,.51f,.51f,.65f,cream);Circle(.5f,.7f,.09f,Teal);Rect(.37f,.49f,.1f,.1f,Purple);Rect(.63f,.49f,.1f,.1f,Purple);foreach(float x in new[]{.34f,.5f,.66f})Circle(x,.31f,.035f,ink); }
            else if (name == "Soon")
            {
                Circle(.5f,.5f,.35f,Gold);Circle(.5f,.5f,.27f,cream);Line(new(.5f,.5f),new(.5f,.69f),.025f,ink);Line(new(.5f,.5f),new(.64f,.42f),.025f,ink);
                Circle(.5f,.5f,.045f,ink);
            }
            tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.spritePixelsPerUnit=100;importer.textureCompression=TextureImporterCompression.Uncompressed;
            if(name=="Round")importer.spriteBorder=new Vector4(64,64,64,64);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
