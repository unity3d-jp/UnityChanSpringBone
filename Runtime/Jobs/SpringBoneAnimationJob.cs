using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;
using Unity.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.Animations;
#else
using UnityEngine.Experimental.Animations;

#endif


namespace Unity.Animations.SpringBones.Jobs
{
    [System.Serializable]
    public struct SpringBoneProperties
    {
        public float stiffnessForce;
        public float dragForce;
        public Vector3 springForce;
        public float windInfluence;
        public float angularStiffness;
        public AngleLimitComponent yAngleLimits;
        public AngleLimitComponent zAngleLimits;
        public float radius;
        public float springLength;
        public Vector3 boneAxis;
        public bool isRootTransform;
    }

    [System.Serializable]
    public struct SpringBoneComponent
    {
        public Quaternion skinAnimationLocalRotation;
        public Quaternion initialLocalRotation;
        public Quaternion actualLocalRotation;
        public Vector3 currentTipPosition;
        public Vector3 previousTipPosition;
    }
    
    public struct SpringBoneJob : IAnimationJob
    {
        public TransformStreamHandle rootHandle;
        public NativeArray<TransformStreamHandle> springBoneTransformHandles;
        public NativeArray<SpringBoneProperties> springBoneProperties; //read only
        public NativeArray<SpringBoneComponent> springBoneComponents;

        // Manager level value
        public bool isPaused;
        public int simulationFrameRate;
        public float dynamicRatio;
        public Vector3 gravity;
        public float bounce;
        public float friction;
        public bool enableAngleLimits;
        public bool enableCollision;
        public bool enableLengthLimits;
        public bool collideWithGround;
        public float groundHeight;

        struct TransformQueryCache
        {
            public Quaternion parentRotation;
            public Vector3 position;
            public Vector3 rotation;
        }
        
        /// <summary>
        /// Transfer the root position and rotation through the graph.
        /// </summary>
        /// <param name="stream">The animation stream</param>
        public void ProcessRootMotion(AnimationStream stream)
        {
            // Get root position and rotation.
            var rootPosition = rootHandle.GetPosition(stream);
            var rootRotation = rootHandle.GetRotation(stream);

            // The root always follow the given position and rotation.
            rootHandle.SetPosition(stream, rootPosition);
            rootHandle.SetRotation(stream, rootRotation);
        }

        /// <summary>
        /// Procedurally generate the joints rotation.
        /// </summary>
        /// <param name="stream">The animation stream</param>
        public void ProcessAnimation(AnimationStream stream)
        {
            if (springBoneTransformHandles.Length < 2)
                return;

            UpdateDynamics(stream);
        }
        
        private void UpdateDynamics(AnimationStream stream)
        {
            var deltaTime = (simulationFrameRate > 0) ? (1f / simulationFrameRate) : stream.deltaTime;

            for (var boneIndex = 0; boneIndex < springBoneComponents.Length; boneIndex++)
            {
                var bone = springBoneComponents[boneIndex];
                var prop = springBoneProperties[boneIndex];

                if (!isPaused)
                {
                    UpdateSpring(ref bone, ref prop, boneIndex, deltaTime, stream);
                    SatisfyConstraintsAndComputeRotation(ref bone, ref prop, boneIndex, deltaTime, stream);
                }

                UpdateRotation(ref bone, ref prop, boneIndex, stream);

                springBoneComponents[boneIndex] = bone;
            }
        }

        private void UpdateSpring(ref SpringBoneComponent bone, ref SpringBoneProperties prop, int index, float deltaTime, AnimationStream stream)
        {
            
            bone.skinAnimationLocalRotation = springBoneTransformHandles[index].GetLocalRotation(stream);

//            var baseWorldRotation = transform.parent.rotation * initialLocalRotation;
            var baseWorldRotation =
                (index == 0
                    ? rootHandle.GetRotation(stream)
                    : springBoneTransformHandles[index - 1].GetRotation(stream)) * bone.initialLocalRotation;

            springBoneTransformHandles[index].GetGlobalTR(stream, out var position, out var rotation);
            
            var orientedInitialPosition = position + baseWorldRotation * prop.boneAxis * prop.springLength;

            // Hooke's law: force to push us to equilibrium
            var force = prop.stiffnessForce * (orientedInitialPosition - bone.currentTipPosition);
            force += prop.springForce;
            var sqrDt = deltaTime * deltaTime;
            force *= 0.5f * sqrDt;

            var temp = bone.currentTipPosition;
            force += (1f - prop.dragForce) * (bone.currentTipPosition - bone.previousTipPosition);
            bone.currentTipPosition += force;
            bone.previousTipPosition = temp;

            // Inlined because FixBoneLength is slow
            var headPosition = position;
            var headToTail = bone.currentTipPosition - headPosition;
            var magnitude = headToTail.magnitude;
            const float MagnitudeThreshold = 0.001f;

            if (magnitude <= MagnitudeThreshold)
            {
                // was originally this
                //headToTail = transform.TransformDirection(boneAxis)
                var localToGlobal = new Matrix4x4();
                localToGlobal.SetTRS(position, rotation, Vector3.one);
                headToTail = localToGlobal * headToTail;
            }
            else
            {
                headToTail /= magnitude;
            }
            
            bone.currentTipPosition = headPosition + prop.springLength * headToTail;
        }

        private void SatisfyConstraintsAndComputeRotation(ref SpringBoneComponent bone, ref SpringBoneProperties prop, 
            int index, float deltaTime, AnimationStream stream)
        {
//            if (enableLengthLimits)
//            {
//                currTipPos = ApplyLengthLimits(deltaTime);
//            }

            var hadCollision = false;

//            if (collideWithGround)
//            {
//                hadCollision = CheckForGroundCollision();
//            }

//            if (enableCollision & !hadCollision)
//            {
//                hadCollision = CheckForCollision();
//            }

            if (enableAngleLimits)
            {
                ApplyAngleLimits(ref bone, ref prop, index, deltaTime, stream);
            }
        }

        private void UpdateRotation(ref SpringBoneComponent bone, ref SpringBoneProperties prop, int index, AnimationStream stream)
        {
            if (float.IsNaN(bone.currentTipPosition.x)
                | float.IsNaN(bone.currentTipPosition.y)
                | float.IsNaN(bone.currentTipPosition.z))
            {
                var parentRotation = rootHandle.GetRotation(stream);
                var position = springBoneTransformHandles[index].GetPosition(stream);
                var baseWorldRotation = parentRotation * bone.initialLocalRotation;
                bone.currentTipPosition = position + baseWorldRotation * prop.boneAxis * prop.springLength;
                bone.previousTipPosition = bone.currentTipPosition;
            }

            bone.actualLocalRotation = ComputeLocalRotation(ref bone, ref prop, index, stream, bone.currentTipPosition);
            var localRotation = Quaternion.Lerp(bone.skinAnimationLocalRotation, bone.actualLocalRotation, dynamicRatio);
            springBoneTransformHandles[index].SetLocalRotation(stream, localRotation);
        }

        private Quaternion ComputeLocalRotation(ref SpringBoneComponent bone, ref SpringBoneProperties prop, int index, AnimationStream stream, Vector3 tipPosition)
        {
            var parentRotation = rootHandle.GetRotation(stream);
            var position = springBoneTransformHandles[index].GetPosition(stream);

            var baseWorldRotation = parentRotation * bone.initialLocalRotation;
            var worldBoneVector = tipPosition - position;
            var localBoneVector = Quaternion.Inverse(baseWorldRotation) * worldBoneVector;
            localBoneVector.Normalize();

            var aimRotation = Quaternion.FromToRotation(prop.boneAxis, localBoneVector);
            var outputRotation = bone.initialLocalRotation * aimRotation;

            return outputRotation;
        }
        
        private void ApplyAngleLimits(ref SpringBoneComponent bone, ref SpringBoneProperties prop, int index, float deltaTime, AnimationStream stream)
        {
            if (!prop.yAngleLimits.active && !prop.zAngleLimits.active)
            {
                return;
            }

            var origin = springBoneTransformHandles[index].GetPosition(stream);
            var vector = bone.currentTipPosition - origin;

            Vector3 pivotPosition;
            Quaternion pivotRotation;

            if (prop.isRootTransform)
            {
                springBoneTransformHandles[index].GetGlobalTR(stream, out pivotPosition, out pivotRotation);
            }
            else if (index == 0) {
                rootHandle.GetGlobalTR(stream, out pivotPosition, out pivotRotation);
            }
            else
            {
                springBoneTransformHandles[index-1].GetGlobalTR(stream, out pivotPosition, out pivotRotation);
            }

            var mat = new Matrix4x4();
            mat.SetTRS(pivotPosition, pivotRotation, Vector3.one);
            
            var forward = mat * -Vector3.right;

            if (prop.yAngleLimits.active)
            {
                prop.yAngleLimits.ConstrainVector(
                    mat * -Vector3.up, mat * -Vector3.forward, forward, prop.angularStiffness, deltaTime, ref vector);
            }

            if (prop.zAngleLimits.active)
            {
                prop.zAngleLimits.ConstrainVector(
                    mat * -Vector3.forward, mat * -Vector3.up, forward, prop.angularStiffness, deltaTime, ref vector);
            }

            bone.currentTipPosition = origin + vector;
        }
    }
}