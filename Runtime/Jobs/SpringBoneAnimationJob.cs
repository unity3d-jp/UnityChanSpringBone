using Unity.Collections;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Animations;

#if !UNITY_2019_3_OR_NEWER
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
        public Quaternion currentLocoalRotation;
        public Vector3 currentTipPosition;
        public Vector3 previousTipPosition;
    }
    
    public struct SpringBoneJob : IAnimationJob
    {
        public TransformStreamHandle rootHandle;
        public NativeArray<TransformStreamHandle> springBoneParentTransformHandles;
        public NativeArray<TransformStreamHandle> springBoneTransformHandles;
        public NativeArray<SpringBoneProperties> springBoneProperties; //read only
        public NativeArray<SpringBoneComponent> springBoneComponents;

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

        private struct TransformQueryCache
        {
            public Vector3 parentPosition;
            public Quaternion parentRotation;
            public Matrix4x4 parentLocalToGlobalMat;

            public Vector3 position;
            public Quaternion rotation;
            public Quaternion localRotation;
            public Matrix4x4 localToGlobalMat;
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

            for (var index = 0; index < springBoneComponents.Length; index++)
            {
                var bone = springBoneComponents[index];
                var prop = springBoneProperties[index];
                
                springBoneParentTransformHandles[index].GetGlobalTR(stream, out var parentPos, out var parentRot);
                springBoneTransformHandles[index].GetGlobalTR(stream, out var pos, out var rot);
                var tc = new TransformQueryCache {
                    parentPosition = parentPos,
                    parentRotation = parentRot,
                    parentLocalToGlobalMat = Matrix4x4.TRS(parentPos, parentRot, Vector3.one),
                    position = pos,
                    rotation = rot,
                    localRotation = springBoneTransformHandles[index].GetLocalRotation(stream),
                    localToGlobalMat = Matrix4x4.TRS(pos, rot, Vector3.one)
                };

                if (!isPaused)
                {
                    UpdateSpring(ref bone, index, deltaTime, in prop, in tc);
                    SatisfyConstraintsAndComputeRotation(ref bone, index, deltaTime, in prop, in tc);
                }

                UpdateRotation(ref bone, index, in prop, in tc);

                springBoneTransformHandles[index].SetLocalRotation(stream, bone.currentLocoalRotation);
                springBoneComponents[index] = bone;
            }
        }

        private void UpdateSpring(ref SpringBoneComponent bone, int index, float deltaTime, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            
            bone.skinAnimationLocalRotation = tc.localRotation;

            var baseWorldRotation = tc.parentRotation * bone.initialLocalRotation;

            var orientedInitialPosition = tc.position + baseWorldRotation * prop.boneAxis * prop.springLength;

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
            var headPosition = tc.position;
            var headToTail = bone.currentTipPosition - headPosition;
            var magnitude = headToTail.magnitude;
            const float MagnitudeThreshold = 0.001f;

            if (magnitude <= MagnitudeThreshold)
            {
                // was originally this
                //headToTail = transform.TransformDirection(boneAxis)
                headToTail = tc.localToGlobalMat * headToTail;
            }
            else
            {
                headToTail /= magnitude;
            }
            
            bone.currentTipPosition = headPosition + prop.springLength * headToTail;
        }

        private void SatisfyConstraintsAndComputeRotation(ref SpringBoneComponent bone, int index, float deltaTime,
            in SpringBoneProperties prop, in TransformQueryCache tc)
        {
//            if (enableLengthLimits)
//            {
//                currTipPos = ApplyLengthLimits(deltaTime);
//            }

//            var hadCollision = false;

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
                ApplyAngleLimits(ref bone, index, deltaTime, in prop, in tc);
            }
        }

        private void UpdateRotation(ref SpringBoneComponent bone, int index, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            if (float.IsNaN(bone.currentTipPosition.x)
                | float.IsNaN(bone.currentTipPosition.y)
                | float.IsNaN(bone.currentTipPosition.z))
            {
//                var parentRotation = rootHandle.GetRotation(stream);
//                var position = springBoneTransformHandles[index].GetPosition(stream);
                var baseWorldRotation = tc.parentRotation * bone.initialLocalRotation;
                bone.currentTipPosition = tc.position + baseWorldRotation * prop.boneAxis * prop.springLength;
                bone.previousTipPosition = bone.currentTipPosition;
            }

            bone.actualLocalRotation = ComputeLocalRotation(ref bone, index, bone.currentTipPosition, in prop, in tc);
            bone.currentLocoalRotation = Quaternion.Lerp(bone.skinAnimationLocalRotation, bone.actualLocalRotation, dynamicRatio);
        }

        private Quaternion ComputeLocalRotation(ref SpringBoneComponent bone, int index, Vector3 tipPosition, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            var baseWorldRotation = tc.parentRotation * bone.initialLocalRotation;
            var worldBoneVector = tipPosition - tc.position;
            var localBoneVector = Quaternion.Inverse(baseWorldRotation) * worldBoneVector;
            localBoneVector.Normalize();

            var aimRotation = Quaternion.FromToRotation(prop.boneAxis, localBoneVector);
            var outputRotation = bone.initialLocalRotation * aimRotation;

            return outputRotation;
        }
        
        private void ApplyAngleLimits(ref SpringBoneComponent bone, int index, float deltaTime, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            if (!prop.yAngleLimits.active && !prop.zAngleLimits.active)
            {
                return;
            }

            var origin = tc.position;
            var vector = bone.currentTipPosition - origin;
            
            var forward = tc.parentLocalToGlobalMat * -Vector3.right;

            if (prop.yAngleLimits.active)
            {
                prop.yAngleLimits.ConstrainVector(
                    tc.parentLocalToGlobalMat * -Vector3.up, tc.parentLocalToGlobalMat * -Vector3.forward, forward, prop.angularStiffness, deltaTime, ref vector);
            }

            if (prop.zAngleLimits.active)
            {
                prop.zAngleLimits.ConstrainVector(
                    tc.parentLocalToGlobalMat * -Vector3.forward, tc.parentLocalToGlobalMat * -Vector3.up, forward, prop.angularStiffness, deltaTime, ref vector);
            }

            bone.currentTipPosition = origin + vector;
        }
    }
}