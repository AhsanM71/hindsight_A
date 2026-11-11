using System;
using UnityEngine;

namespace EyeCanDo
{
    public sealed class JourneyMapLayout : MonoBehaviour
    {
        public JourneyCatalog catalog;
        public Transform destinations;
        public Transform paths;
        public GameObject pathPebble;
        public Camera mapCamera;
        [Serializable]
        public sealed class Placement
        {
            public string journeyId;
            public Vector3 position;
            public GameObject presentationPrefab;
        }
        [Tooltip("Map-only presentation overrides, keyed by stable catalog ID.")]
        public Placement[] authoredPlacements = Array.Empty<Placement>();
        public bool curvedPaths;
        public Material pathMaterial;
        public float pathWidth = 1.5f;
        public bool fitPresentationBounds;
        public Bounds compositionBounds = new Bounds(new Vector3(0, 2, 3), new Vector3(30, 8, 18));
        [Tooltip("Screen space reserved for the world, excluding header and footer.")]
        public Rect worldViewport = new Rect(.04f, .12f, .92f, .76f);
        public Vector3 cameraFocus = new Vector3(0, 1, 2);
        float aspect;
        public Vector3 PositionFor(int index)
        {
            var entry = Array.Find(authoredPlacements, p => p.journeyId == catalog.journeys[index].id);
            if (entry != null) return entry.position;
            // Unauthored entries form an additional row, keeping authored destinations in place.
            if (authoredPlacements.Length > 0)
            {
                int automatic = 0, total = 0;
                for (int i = 0; i < catalog.journeys.Length; i++)
                    if (!Array.Exists(authoredPlacements, p => p.journeyId == catalog.journeys[i].id))
                    { if (i < index) automatic++; total++; }
                return new Vector3((automatic - (total - 1) * .5f) * 7.2f, 0, -7);
            }
            return new Vector3((index - (catalog.journeys.Length - 1) * .5f) * 7.2f, 0, index % 2 == 0 ? 0 : .8f);
        }
        GameObject PrefabFor(int index)
        {
            var entry = Array.Find(authoredPlacements, p => p.journeyId == catalog.journeys[index].id);
            return entry != null && entry.presentationPrefab != null ? entry.presentationPrefab : catalog.journeys[index].destinationPrefab;
        }
        public void Rebuild()
        {
            Clear(destinations); Clear(paths);
            int count = catalog.journeys.Length;
            for (int i = 0; i < count; i++)
            {
                float x = PositionFor(i).x;
                var destination = Instantiate(PrefabFor(i), destinations);
                destination.transform.localPosition = PositionFor(i);
                destination.name = catalog.journeys[i].id;
                destination.GetComponent<JourneyDestination>().definition = catalog.journeys[i];
                if (i == count - 1) continue;
                if (curvedPaths)
                {
                    Vector3 a = PositionFor(i), b = PositionFor(i + 1);
                    a.y = b.y = .11f;
                    var path = new GameObject("Winding path " + i, typeof(MeshFilter), typeof(MeshRenderer));
                    path.transform.SetParent(paths, false);
                    path.GetComponent<MeshFilter>().sharedMesh = GardenPathMesh.Create(a, a + new Vector3(2, 0, -3), b + new Vector3(-2, 0, -3), b, pathWidth);
                    if (Application.isPlaying) path.AddComponent<GardenPathLifetime>().runtimeMesh = path.GetComponent<MeshFilter>().sharedMesh;
                    path.GetComponent<MeshRenderer>().sharedMaterial = pathMaterial;
                    continue;
                }
                for (int p = 1; p <= 8; p++)
                {
                    var pebble = Instantiate(pathPebble, paths);
                    float t = p / 9f;
                    pebble.transform.localPosition = new Vector3(x + 7.2f * t, .05f, -1.4f + Mathf.Sin(t * Mathf.PI) * -.4f);
                }
            }
            FitCamera();
        }
        static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying) { child.SetActive(false); Destroy(child); } else DestroyImmediate(child);
            }
        }
        void Start()
        {
            var existing = destinations.GetComponentsInChildren<JourneyDestination>();
            bool matches = existing.Length == catalog.journeys.Length;
            for (int i = 0; matches && i < existing.Length; i++) matches = existing[i].definition == catalog.journeys[i];
            if (!matches) Rebuild();
            FitCamera();
        }
        void Update() { if (!Mathf.Approximately(aspect, mapCamera.aspect)) FitCamera(); }
        public void FitCamera()
        {
            if (mapCamera == null || catalog == null) return;
            aspect = mapCamera.aspect;
            if (fitPresentationBounds)
            {
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                void Include(Vector3 world)
                {
                    var local = Quaternion.Inverse(mapCamera.transform.rotation) * (world - cameraFocus);
                    min = Vector2.Min(min, new Vector2(local.x, local.y));
                    max = Vector2.Max(max, new Vector2(local.x, local.y));
                }
                void IncludeBounds(Bounds bounds)
                {
                    for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        Include(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                }
                IncludeBounds(compositionBounds);
                foreach (var renderer in destinations.GetComponentsInChildren<Renderer>()) IncludeBounds(renderer.bounds);
                var corners = new Vector3[4];
                foreach (var rect in destinations.GetComponentsInChildren<RectTransform>())
                { rect.GetWorldCorners(corners); foreach (var corner in corners) Include(corner); }
                mapCamera.orthographicSize = Mathf.Max((max.y - min.y) / (2 * worldViewport.height), (max.x - min.x) / (2 * aspect * worldViewport.width)) + .12f;
                Vector2 center = (min + max) * .5f;
                center -= (worldViewport.center - Vector2.one * .5f) * new Vector2(2 * mapCamera.orthographicSize * aspect, 2 * mapCamera.orthographicSize);
                mapCamera.transform.position = cameraFocus + mapCamera.transform.right * center.x + mapCamera.transform.up * center.y - mapCamera.transform.forward * 40;
                return;
            }
            mapCamera.orthographicSize = Mathf.Max(7.2f, (catalog.journeys.Length * 7.2f + 2.5f) / (2 * aspect));
        }
    }
}
