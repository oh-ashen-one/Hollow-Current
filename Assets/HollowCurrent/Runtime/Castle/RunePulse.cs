using UnityEngine;

namespace HollowCurrent
{
    // Moved out of RuneRing.cs by piece 0: a MonoBehaviour saved in a prepared scene
    // needs its own script file named after the class.
    // Slow breathing of the shared violet rune material (sarcophagus runes). One material, one colour write per frame.
    public sealed class RunePulse : MonoBehaviour
    {
        public Color dim=new Color(.42f,.2f,.72f,1),bright=new Color(.82f,.48f,1f,1);
        public float hertz=.22f;
        Material material;
        void Start(){material=SurrealProps.VioletRuneMaterial;}
        void Update()
        {
            if(!material)return;
            material.color=Color.Lerp(dim,bright,.5f+.5f*Mathf.Sin(Time.time*hertz*Mathf.PI*2));
        }
    }
}
