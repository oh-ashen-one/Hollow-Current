using UnityEngine;

namespace HollowCurrent
{
    // The drawbridge chains are a static mesh posed for the lowered leaf: show them only while the bridge is down.
    public sealed class ChainVisibility : MonoBehaviour
    {
        public CastleDrawbridge bridge;
        public GameObject chains;
        bool shown=true;
        void Update()
        {
            if(!bridge||!chains)return;
            bool want=bridge.Lowered&&!bridge.Moving;
            if(want!=shown){shown=want;chains.SetActive(want);}
        }
    }
}
