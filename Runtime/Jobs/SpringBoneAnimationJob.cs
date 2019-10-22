using Unity.Collections;
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
        public SpringCollisionLayerMask layer;
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
    
    [System.Serializable]
    public struct SpringColliderTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Matrix4x4 worldToLocalMatrix;
        public Matrix4x4 localToWorldMatrix;
    }

    public struct SpringBoneJob : IAnimationJob
    {
        public TransformStreamHandle rootHandle;
        public NativeArray<TransformStreamHandle> springBoneParentTransformHandles;
        public NativeArray<TransformStreamHandle> springBoneTransformHandles;
        [ReadOnly]
        public NativeArray<SpringBoneProperties> springBoneProperties;
        public NativeArray<SpringBoneComponent> springBoneComponents;

        public NativeArray<TransformStreamHandle> springColliderTransformHandles;
        [ReadOnly]
        public NativeArray<SpringColliderComponent> colliders;
        public NativeArray<SpringColliderTransform> colliderTransforms;

        public bool isPaused;
        public int simulationFrameRate;
        public float dynamicRatio;

        public bool enableAngleLimits;
        public bool enableCollision;
        public bool enableLengthLimits;
        public bool collideWithGround;

        public Vector3 gravity;
        public float bounce;
        public float friction;
        public float groundHeight;

        private struct TransformQueryCache
        {
            public Vector3 parentPosition;
            public Quaternion parentRotation;
            public Matrix4x4 parentLocalToGlobalMat;

            public Vector3 position;
            public Quaternion rotation;
            public Quaternion localRotation;
            public Matrix4x4 localToWorldMatrix;
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

            UpdateColliderTransforms(stream);
            UpdateDynamics(stream);
        }

        private void UpdateColliderTransforms(AnimationStream stream)
        {
            for (var i = 0; i < colliders.Length; ++i)
            {
                var ct = colliderTransforms[i];
                springColliderTransformHandles[i].GetGlobalTR(stream, out ct.position, out ct.rotation);

                colliderTransforms[i] = new SpringColliderTransform
                {
                    //TODO
                };
            }
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
                    localToWorldMatrix = Matrix4x4.TRS(pos, rot, Vector3.one)
                };

                if (!isPaused)
                {
                    UpdateSpring(ref bone, index, deltaTime, in prop, in tc);
                    ResolveCollisionsAndConstraints(ref bone, index, deltaTime, in prop, in tc);
                }

                UpdateRotation(ref bone, index, in prop, in tc);

                springBoneTransformHandles[index].SetLocalRotation(stream, bone.currentLocoalRotation);
                springBoneComponents[index] = bone;
            }
        }

        private static void UpdateSpring(ref SpringBoneComponent bone, int index, float deltaTime, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            
            bone.skinAnimationLocalRotation = tc.localRotation;

            var baseWorldRotation = tc.parentRotation * bone.initialLocalRotation;
            var baseWorldAxis = baseWorldRotation * prop.boneAxis;

            var orientedInitialPosition = tc.position + 
                                          baseWorldAxis * prop.springLength;

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
                headToTail = tc.localToWorldMatrix * headToTail;
            }
            else
            {
                headToTail /= magnitude;
            }
            
            bone.currentTipPosition = headPosition + prop.springLength * headToTail;
        }

        private void ResolveCollisionsAndConstraints(ref SpringBoneComponent bone, int index, float deltaTime,
            in SpringBoneProperties prop, in TransformQueryCache tc)
        {
//            if (enableLengthLimits)
//            {
//                bone.currentTipPosition = ApplyLengthLimits(ref bone, index, in prop, in tc, deltaTime);
//            }

            var hadCollision = false;

            if (collideWithGround)
            {
                hadCollision = ResolveGroundCollision(ref bone, index, in prop, in tc);
            }

            if (enableCollision & !hadCollision)
            {
                hadCollision = ResolveCollisions(ref bone, index, in prop, in tc);
            }

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
                var baseWorldRotation = tc.parentRotation * bone.initialLocalRotation;
                var baseWorldAxis = baseWorldRotation * prop.boneAxis;
                bone.currentTipPosition = tc.position + baseWorldAxis * prop.springLength;
                bone.previousTipPosition = bone.currentTipPosition;
            }

            bone.actualLocalRotation = ComputeLocalRotation(ref bone, index, bone.currentTipPosition, in prop, in tc);
            bone.currentLocoalRotation = Quaternion.Lerp(bone.skinAnimationLocalRotation, bone.actualLocalRotation, dynamicRatio);
        }

        private static Quaternion ComputeLocalRotation(ref SpringBoneComponent bone, int index, Vector3 tipPosition, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            var baseWorldRotation = tc.parentRotation * bone.initialLocalRotation;
            var worldBoneVector = tipPosition - tc.position;
            var localBoneVector = Quaternion.Inverse(baseWorldRotation) * worldBoneVector;
            localBoneVector.Normalize();

            var aimRotation = Quaternion.FromToRotation(prop.boneAxis, localBoneVector);
            var outputRotation = bone.initialLocalRotation * aimRotation;

            return outputRotation;
        }
        
        private static void ApplyAngleLimits(ref SpringBoneComponent bone, int index, float deltaTime, in SpringBoneProperties prop, in TransformQueryCache tc)
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
                vector = prop.yAngleLimits.ConstrainVector(
                    vector,
                    tc.parentLocalToGlobalMat * -Vector3.up, 
                    tc.parentLocalToGlobalMat * -Vector3.forward, forward, prop.angularStiffness, deltaTime);
            }

            if (prop.zAngleLimits.active)
            {
                vector = prop.zAngleLimits.ConstrainVector(
                    vector,
                    tc.parentLocalToGlobalMat * -Vector3.forward, 
                    tc.parentLocalToGlobalMat * -Vector3.up, forward, prop.angularStiffness, deltaTime);
            }

            bone.currentTipPosition = origin + vector;
        }
        
        // 
        // Collisions
        //
        
//        private Vector3 ApplyLengthLimits(ref SpringBoneComponent bone, int index, in SpringBoneProperties prop, in TransformQueryCache tc, float deltaTime)
//        {
//            var targetCount = lengthLimitTargets.Length;
//            if (targetCount == 0)
//            {
//                return currTipPos;
//            }
//
//            const float SpringConstant = 0.5f;
//            var accelerationMultiplier = SpringConstant * deltaTime * deltaTime;
//            var movement = new Vector3(0f, 0f, 0f);
//            for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
//            {
//                var targetPosition = lengthLimitTargets[targetIndex].position;
//                var lengthToLimitTarget = lengthsToLimitTargets[targetIndex];
//                var currentToTarget = currTipPos - targetPosition;
//                var currentDistanceSquared = currentToTarget.sqrMagnitude;
//
//                // Hooke's Law
//                var currentDistance = Mathf.Sqrt(currentDistanceSquared);
//                var distanceFromEquilibrium = currentDistance - lengthToLimitTarget;
//                movement -= accelerationMultiplier * distanceFromEquilibrium * currentToTarget.normalized;
//            }
//
//            return currTipPos + movement;
//        }
        
        private bool ResolveCollisions(ref SpringBoneComponent bone, int index, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            var desiredPosition = bone.currentTipPosition;
            var headPosition = tc.position;
            
//            var scaledRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;
            // var scaleMagnitude = new Vector3(prop.radius, 0f, 0f).magnitude;
            var hitNormal = new Vector3(0f, 0f, 1f);

            var hadCollision = false;

            for (var i = 0; i < colliders.Length; ++i)
            {
                var collider = colliders[i];
                var colliderTransform = colliderTransforms[i];

                if ((collider.layer & prop.layer) == 0)
                {
                    continue;
                }
                
                switch (collider.type)
                {
                    case ColliderType.Capsule:
                        hadCollision |= SpringCollisionResolver.ResolveCapsule(
                            collider, colliderTransform,
                            headPosition, ref bone.currentTipPosition, ref hitNormal, 
                            prop.radius);
                        break;
                    case ColliderType.Sphere:
                        hadCollision |= SpringCollisionResolver.ResolveSphere(
                            collider, colliderTransform,
                            headPosition, ref bone.currentTipPosition, ref hitNormal, prop.radius);
                        break;
                    case ColliderType.Panel:
                        hadCollision |= SpringCollisionResolver.ResolvePanel(
                            collider, colliderTransform,
                            headPosition, ref bone.currentTipPosition, ref hitNormal, prop.springLength, 
                            prop.radius);
                        break;
                }
            }

            if (hadCollision)
            {
                var incidentVector = desiredPosition - bone.previousTipPosition;
                var reflectedVector = Vector3.Reflect(incidentVector, hitNormal);

                // friction
                var upwardComponent = Vector3.Dot(reflectedVector, hitNormal) * hitNormal;
                var lateralComponent = reflectedVector - upwardComponent;

                var bounceVelocity = bounce * upwardComponent + (1f - friction) * lateralComponent;
                const float BounceThreshold = 0.0001f;
                if (bounceVelocity.sqrMagnitude > BounceThreshold)
                {
                    var distanceTraveled = (bone.currentTipPosition - bone.previousTipPosition).magnitude;
                    bone.previousTipPosition = bone.currentTipPosition - bounceVelocity;
                    bone.currentTipPosition += Mathf.Max(0f, bounceVelocity.magnitude - distanceTraveled) * bounceVelocity.normalized;
                }
                else
                {
                    bone.previousTipPosition = bone.currentTipPosition;
                }
            }
            return hadCollision;
        }

        private bool ResolveGroundCollision(ref SpringBoneComponent bone, int index, in SpringBoneProperties prop, in TransformQueryCache tc)
        {
            // Todo: this assumes a flat ground parallel to the xz plane
            var worldHeadPosition = tc.position;
            var worldTailPosition = bone.currentTipPosition;
//            var worldRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;
            var worldLength = (bone.currentTipPosition - worldHeadPosition).magnitude;
            worldHeadPosition.y -= groundHeight;
            worldTailPosition.y -= groundHeight;

            var collidingWithGround = SpringCollisionResolver.ResolvePanelOnAxis(
                worldHeadPosition,
                ref worldTailPosition, 
                worldLength, prop.radius, SpringCollisionResolver.Axis.Y);

            if (collidingWithGround)
            {
                worldTailPosition.y += groundHeight;
                bone.currentTipPosition = FixBoneLength(
                    in tc,
                    in prop,
                    tc.position,
                    worldTailPosition, 0.5f * prop.springLength, prop.springLength);
                // Todo: bounce, friction
                bone.previousTipPosition = bone.currentTipPosition;
            }

            return collidingWithGround;
        }

        private static Vector3 FixBoneLength(in TransformQueryCache tc, in SpringBoneProperties prop, 
            Vector3 headPosition, Vector3 tailPosition, float minLength, float maxLength) 
        {
            var headToTail = tailPosition - headPosition;
            var magnitude = headToTail.magnitude;
            const float MagnitudeThreshold = 0.001f;
            if (magnitude <= MagnitudeThreshold)
            {
                var AxisVector = tc.localToWorldMatrix * prop.boneAxis * minLength;
                
//                return headPosition + transform.TransformDirection(boneAxis) * minLength;
                return new Vector3
                {
                    x = headPosition.x + AxisVector.x,
                    y = headPosition.y + AxisVector.y,
                    z = headPosition.z + AxisVector.z
                };
            }

            var newMagnitude = (magnitude < minLength) ? minLength : magnitude;
            newMagnitude = (newMagnitude > maxLength) ? maxLength : newMagnitude;
            return headPosition + (newMagnitude / magnitude) * headToTail;
        }        
    }
}