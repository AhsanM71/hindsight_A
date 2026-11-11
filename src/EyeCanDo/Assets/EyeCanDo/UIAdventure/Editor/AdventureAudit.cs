using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EyeCanDo.Editor
{
    public static class AdventureAudit
    {
        public const string Folder = "Validation/UIIteration3";
        [Serializable] class Record { public string path; public Vector3 position, scale; public Quaternion rotation; public bool active; public string[] components, materials; public string mesh, camera, light, collider; }
        [Serializable] class Snapshot { public string scene, ambientMode; public Color ambientSky, ambientEquator, ambientGround, ambientLight; public float ambientIntensity; public bool fog; public string[] volumes; public List<Record> objects = new(); }
        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.GetSiblingIndex() + ":" + t.name;
        static string Ref(UnityEngine.Object o)
        {
            if (o == null) return "null";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long id)) return guid + ":" + id;
            return o.name;
        }
        public static string SnapshotMap()
        {
            var s = new Snapshot { scene = SceneManager.GetActiveScene().path, ambientMode = RenderSettings.ambientMode.ToString(), ambientSky = RenderSettings.ambientSkyColor, ambientEquator = RenderSettings.ambientEquatorColor, ambientGround = RenderSettings.ambientGroundColor, ambientLight = RenderSettings.ambientLight, ambientIntensity = RenderSettings.ambientIntensity, fog = RenderSettings.fog, volumes = UnityEngine.Object.FindObjectsByType<Volume>().Select(v => Ref(v.sharedProfile)).OrderBy(v=>v).ToArray() };
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name != "Screen UI" && g.name != "Map Avatar" && g.GetComponent<AppRoot>() == null && g.GetComponent<JourneyMapController>() == null && g.GetComponent<UnityEngine.EventSystems.EventSystem>() == null))
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var mesh = t.GetComponent<MeshFilter>(); var renderer = t.GetComponent<Renderer>(); var camera = t.GetComponent<Camera>(); var light = t.GetComponent<Light>(); var box = t.GetComponent<BoxCollider>();
                s.objects.Add(new Record { path = PathOf(t), position = t.localPosition, rotation = t.localRotation, scale = t.localScale, active = t.gameObject.activeSelf, components = t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray(), mesh = t.GetComponent<TMP_Text>() == null ? Ref(mesh != null ? mesh.sharedMesh : null) : "approved typography", materials = renderer != null && t.GetComponent<TMP_Text>() == null ? renderer.sharedMaterials.Select(Ref).ToArray() : Array.Empty<string>(), camera = camera == null ? null : JsonUtility.ToJson(new CameraRecord(camera)), light = light == null ? null : JsonUtility.ToJson(new LightRecord(light)), collider = box == null ? null : JsonUtility.ToJson(new BoxRecord(box)) });
            }
            s.objects = s.objects.OrderBy(x=>x.path).ToList(); return JsonUtility.ToJson(s, true);
        }
        [Serializable] class CameraRecord {public float size, near, far, aspect; public Color background;public int mask, clear; public bool ortho,hdr; public CameraRecord(Camera c){size=c.orthographicSize;near=c.nearClipPlane;far=c.farClipPlane;aspect=c.aspect;background=c.backgroundColor;mask=c.cullingMask;clear=(int)c.clearFlags;ortho=c.orthographic;hdr=c.allowHDR;}}
        [Serializable] class LightRecord {public Color color;public float intensity,strength;public int shadows,type;public LightRecord(Light l){color=l.color;intensity=l.intensity;strength=l.shadowStrength;shadows=(int)l.shadows;type=(int)l.type;}}
        [Serializable] class BoxRecord {public Vector3 center,size;public bool trigger;public BoxRecord(BoxCollider b){center=b.center;size=b.size;trigger=b.isTrigger;}}
        public static void Baseline()
        {
            Directory.CreateDirectory(Folder); if (!File.Exists(Folder+"/map-before.json")) File.WriteAllText(Folder+"/map-before.json", SnapshotMap());
        }
        public static bool Compare()
        {
            var current = SnapshotMap(); File.WriteAllText(Folder+"/map-after.json",current);
            bool equal = current == File.ReadAllText(Folder+"/map-before.json");
            File.WriteAllText(Folder+"/map-comparison.json", "{\"unchangedWorldCameraAndLighting\":"+equal.ToString().ToLowerInvariant()+"}");
            if (!equal) throw new InvalidOperationException("Map environment differs from baseline. Inspect map-before and map-after.");
            return equal;
        }
    }
}
