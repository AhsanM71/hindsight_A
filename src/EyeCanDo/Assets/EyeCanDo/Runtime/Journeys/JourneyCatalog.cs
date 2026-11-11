using System;
using UnityEngine;

namespace EyeCanDo
{
    [CreateAssetMenu(menuName = "EyeCanDo/Journey Catalog")]
    public sealed class JourneyCatalog : ScriptableObject
    {
        public JourneyDefinition[] journeys = Array.Empty<JourneyDefinition>();
        public JourneyDefinition Find(string id) => Array.Find(journeys, x => x != null && x.id == id);
    }
}
