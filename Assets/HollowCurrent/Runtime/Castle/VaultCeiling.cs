using UnityEngine;

namespace HollowCurrent
{
    // Marker for the great hall's vault ceiling mesh. Any castle object with a MonoBehaviour is kept out of static GI (see
    // HouseLighting.Movable), so the vault is lit by light probes, the realtime lights and the look builder's vault fill instead
    // of a bake that leaves the ceiling black.
    public sealed class VaultCeiling : MonoBehaviour
    {
    }
}
