using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent
{
    // The same original, textured assets are used in the hand and on the van rack.
    // Visual meshes have no colliders; equipment behavior remains in Investigator.
    public static class FieldKitArt
    {
        public static readonly string[] Names={"InductionReader","UVLamp","EVPRecorder","Flashlight","FieldRadio"};
        public static Transform Create(Transform parent,string name,float length,Vector3 position,Quaternion rotation,bool handheld=true)
        {
            var prefab=Resources.Load<GameObject>("Furniture/"+name);
            if(!prefab)throw new InvalidOperationException("Required original field-kit asset missing: "+name);
            var root=new GameObject("Field kit art / "+name).transform;
            root.SetParent(parent,false);root.localPosition=position;root.localRotation=rotation;
            var visual=UnityEngine.Object.Instantiate(prefab,root,false).transform;
            var filters=visual.GetComponentsInChildren<MeshFilter>();
            if(filters.Length==0)throw new InvalidOperationException("Field-kit asset has no mesh: "+name);
            var bounds=filters[0].sharedMesh.bounds;
            for(int i=1;i<filters.Length;i++)bounds.Encapsulate(filters[i].sharedMesh.bounds);
            float largest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            float scale=length/Mathf.Max(.001f,largest);
            visual.localScale=Vector3.one*scale;visual.localPosition=-bounds.center*scale;
            foreach(var r in visual.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode=handheld?ShadowCastingMode.Off:ShadowCastingMode.On;
                r.receiveShadows=!handheld;
                if(handheld)r.gameObject.layer=30;
                if(!r.sharedMaterial||!r.sharedMaterial.mainTexture)throw new InvalidOperationException("Missing field-kit material/texture: "+name);
            }
            if(!handheld)
            {
                float bottom=float.PositiveInfinity;
                foreach(var r in visual.GetComponentsInChildren<Renderer>())bottom=Mathf.Min(bottom,r.bounds.min.y);
                root.position+=Vector3.up*(parent.TransformPoint(position).y-bottom);
            }
            return root;
        }
        public static int VertexCount(Transform item)
        {
            int count=0;foreach(var f in item.GetComponentsInChildren<MeshFilter>())count+=f.sharedMesh.vertexCount;return count;
        }
    }
}
