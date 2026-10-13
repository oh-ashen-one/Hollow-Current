using UnityEngine;
using UnityEngine.Rendering;
namespace HollowCurrent
{
    // Hierarchical articulated glove rig. Original Blender-authored leather,
    // stitching and cuff meshes are shared between both hands and radio pose.
    public sealed class FirstPersonHands : MonoBehaviour
    {
        Transform indexJoint;
        Quaternion indexRest;
        float press;
        public static FirstPersonHands Attach(Transform parent,Vector3 position,Vector3 rotation,bool left=false)
        {
            var go=new GameObject(left?"Left articulated glove":"Right articulated glove");go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(rotation);
            var rig=go.AddComponent<FirstPersonHands>();rig.Create(left);return rig;
        }
        void Create(bool left)
        {
            AuthoredArt.Place(transform,"GlovePalm",Vector3.zero);
            AuthoredArt.Place(transform,"JacketSleeve",new Vector3(0,-.034f,0),Quaternion.Euler(0,0,-25),new Vector3(.88f,1,.88f));
            float[] lengths={1,1.07f,1,.82f};
            for(int i=0;i<4;i++)
            {
                var proximal=Joint("Finger "+i+" proximal",transform,new Vector3(-.026f+i*.0175f,.038f,0),new Vector3(-18-i*2,0,0));
                proximal.localScale=Vector3.one*lengths[i];AuthoredArt.Place(proximal,"GloveFinger",Vector3.zero);
                var middle=Joint("Finger "+i+" middle",proximal,new Vector3(0,.031f,0),new Vector3(-55,0,0));AuthoredArt.Place(middle,"GloveFinger",Vector3.zero,Quaternion.identity,new Vector3(.93f,.75f,.93f));
                var tip=Joint("Finger "+i+" distal",middle,new Vector3(0,.023f,0),new Vector3(-48,0,0));AuthoredArt.Place(tip,"GloveTip",Vector3.zero);
                if(i==0){indexJoint=proximal;indexRest=proximal.localRotation;}
            }
            var thumb=Joint("Opposed thumb",transform,new Vector3(-.032f,-.018f,-.004f),new Vector3(-28,28,-62));
            AuthoredArt.Place(thumb,"GloveFinger",Vector3.zero,Quaternion.identity,new Vector3(1.2f,1.1f,1.15f));
            var thumbTip=Joint("Thumb distal",thumb,new Vector3(0,.032f,0),new Vector3(-54,0,0));AuthoredArt.Place(thumbTip,"GloveTip",Vector3.zero,Quaternion.identity,new Vector3(1.15f,1.1f,1.15f));
            if(left)transform.localScale=new Vector3(-1,1,1);
            foreach(var r in GetComponentsInChildren<Renderer>())
            {r.gameObject.layer=30;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        }
        static Transform Joint(string name,Transform parent,Vector3 p,Vector3 euler)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;t.localRotation=Quaternion.Euler(euler);return t;}
        public void Operate(bool held)
        {
            press=Mathf.MoveTowards(press,held?1:0,Time.deltaTime*7);
            indexJoint.localRotation=indexRest*Quaternion.Euler(-press*14,0,0);
        }
    }
}
