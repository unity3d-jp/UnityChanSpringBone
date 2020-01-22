using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    // Up is y-axis
    public static partial class SpringCollisionResolver
    {
        public static bool ResolveCapsule
        (
            SpringColliderComponent capsule,
            SpringColliderTransform transform,
            Vector3 moverHeadPosition, 
            ref Vector3 moverPosition, 
            ref Vector3 hitNormal,
            float moverRadius
        )
        {
            var radiusScale = transform.worldToLocalMatrix.MultiplyVector(new Vector3(1f, 0f, 0f)).magnitude;

            // Lower than start cap
            var localHeadPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(moverHeadPosition);
            var localMoverPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(moverPosition);
            var localMoverRadius = moverRadius * radiusScale;

            var moverIsAboveTop = localMoverPosition.y >= capsule.height;
            var useSphereCheck = (localMoverPosition.y <= 0f) | moverIsAboveTop;
            float combinedRadius;
            
            if (useSphereCheck)
            {
                var sphereOrigin = new Vector3(0f, moverIsAboveTop ? capsule.height : 0f, 0f);
                combinedRadius = localMoverRadius + capsule.radius;
                if ((localMoverPosition - sphereOrigin).sqrMagnitude >= combinedRadius * combinedRadius)
                {
                    // Not colliding
                    return false;
                }

                var originToHead = localHeadPosition - sphereOrigin;
                var isHeadEmbedded = originToHead.sqrMagnitude <= capsule.radius * capsule.radius;
                
                if (isHeadEmbedded)
                {
                    // The head is inside the sphere, so just try to push the tail out
                    var localHitNormal = (localMoverPosition - sphereOrigin).normalized;
                    localMoverPosition = sphereOrigin + localHitNormal * combinedRadius;
                    moverPosition = transform.localToWorldMatrix.MultiplyPoint3x4(localMoverPosition);
                    hitNormal = (transform.localToWorldMatrix.MultiplyPoint3x4(localHitNormal)).normalized;
                    return true;
                }

                var localHeadRadius = (localMoverPosition - localHeadPosition).magnitude;
                if (ComputeIntersection_Sphere(
                    localHeadPosition, localHeadRadius,
                    sphereOrigin, combinedRadius,
                    out var intersection))
                {
                    localMoverPosition = ComputeNewTailPosition_Sphere(intersection, localMoverPosition);
                    moverPosition = transform.localToWorldMatrix.MultiplyPoint3x4(localMoverPosition);
                    var localHitNormal = (localMoverPosition - sphereOrigin).normalized;
                    hitNormal = (transform.localToWorldMatrix.MultiplyPoint3x4(localHitNormal)).normalized;
                }

                return true;
            }

            var originToMover = new Vector2(localMoverPosition.x, localMoverPosition.z);
            combinedRadius = capsule.radius + localMoverRadius;
            var collided = originToMover.sqrMagnitude <= combinedRadius * combinedRadius;
            if (collided)
            {
                var normal = originToMover.normalized;
                originToMover = combinedRadius * normal;
                var newLocalMoverPosition = new Vector3(originToMover.x, localMoverPosition.y, originToMover.y);
                moverPosition = transform.localToWorldMatrix.MultiplyPoint3x4(newLocalMoverPosition);
                hitNormal = transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(normal.x, 0f, normal.y)).normalized;
            }

            return collided;
        }
    }
}