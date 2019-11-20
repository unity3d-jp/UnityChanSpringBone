using Unity.Animations.SpringBones.Jobs;
using UnityEngine;

namespace Unity.Animations.SpringBones
{
    public class ForceProvider : MonoBehaviour
    {
        public virtual Vector3 GetForceOnBone(SpringBone springBone)
        {
            return Vector3.forward;
        }

        private void OnEnable()
        {
            SpringBoneForceManager.GetManager().RegisterForce(this);
        }

        private void OnDisable()
        {
            SpringBoneForceManager.GetManager().UnRegisterForce(this);
        }
    }
}