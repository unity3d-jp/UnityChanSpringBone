using UnityEngine;

namespace Unity.Animations.SpringBones
{
    // スプリングボーン用の力を与えるベースクラス
    public class ForceProvider : MonoBehaviour
    {
        public virtual Vector3 GetForceOnBone(SpringBone springBone)
        {
            return Vector3.zero;
        }
    }
}