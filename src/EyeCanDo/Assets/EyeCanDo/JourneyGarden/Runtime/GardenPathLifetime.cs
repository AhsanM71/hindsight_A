using System;
using UnityEngine;

namespace EyeCanDo
{
    // Runtime catalog rebuilds own their generated meshes; authored mesh assets are never destroyed.
    public sealed class GardenPathLifetime : MonoBehaviour
    {
        [NonSerialized] public Mesh runtimeMesh;
        void OnDestroy() { if (runtimeMesh != null) Destroy(runtimeMesh); }
    }
}
