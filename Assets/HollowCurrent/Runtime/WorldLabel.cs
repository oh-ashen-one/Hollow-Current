using UnityEngine;
namespace HollowCurrent
{
    // Keep the dynamic font atlas bound to a depth-tested world material.
    // The default GUI text shader draws through the van and house walls.
    public sealed class WorldLabel : MonoBehaviour
    {
        TextMesh label;
        Material material;
        void OnEnable()
        {
            label=GetComponent<TextMesh>();label.text=label.text.Replace("SAFE POINT","FIELD STATION");
            material=new Material(Resources.Load<Shader>("WorldText")){name="World label / runtime font atlas"};
            GetComponent<MeshRenderer>().sharedMaterial=material;
            Font.textureRebuilt+=AtlasChanged;
            label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);
            AtlasChanged(label.font);
        }
        void AtlasChanged(Font font)
        {if(label&&font==label.font&&material)material.mainTexture=font.material.mainTexture;}
        void OnDisable()
        {
            Font.textureRebuilt-=AtlasChanged;
            if(material)Destroy(material);
        }
    }
}
