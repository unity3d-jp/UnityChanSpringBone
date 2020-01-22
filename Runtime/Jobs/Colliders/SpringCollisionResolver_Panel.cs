using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    // Up is y-axis
    public static partial class SpringCollisionResolver
    {

//        public Vector3 GetPlaneNormal()
//        {
//            return transform.forward;
//        }

        public static bool ResolvePanel
        (
            SpringColliderComponent panel,
            SpringColliderTransform transform,
            Vector3 headPosition,
            ref Vector3 tailPosition,
            ref Vector3 hitNormal,
            float length,
            float tailRadius
        )
        {
            var localTailPosition = transform.localToWorldMatrix.MultiplyPoint3x4(tailPosition);
            
            // Plane transform is z-up. if hence z >= tailRadius, there is no collision.  
            if (localTailPosition.z >= tailRadius)
            {
                return false;
            }

            var localHeadPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(headPosition);

            var halfWidth = panel.width / 2f;
            var halfHeight = panel.height /2f;

            var pointOnPlane = Vector3.Lerp(localHeadPosition, localTailPosition,
                Mathf.Clamp01(localHeadPosition.z/(localHeadPosition.z - localTailPosition.z)));
            
            if (Mathf.Abs(pointOnPlane.x) >= halfWidth + tailRadius || 
                Mathf.Abs(pointOnPlane.y) >= halfHeight + tailRadius)
            {
                return false;
            }

            // Check edges
            // SpringBone is entirely over plane (only sphere is crossing)
            if (localHeadPosition.z <= 0f && localTailPosition.z <= 0f)
            {
                if (Mathf.Abs(localHeadPosition.y) > halfHeight)
                {
                    halfHeight = (localTailPosition.y < 0f) ? -halfHeight : halfHeight;
                    localTailPosition = new Vector3(localTailPosition.x, halfHeight, localTailPosition.z);
                }
                else if (Mathf.Abs(localHeadPosition.x) > halfWidth)
                {
                    halfWidth = (localTailPosition.x < 0f) ? -halfWidth : halfWidth;
                    localTailPosition = new Vector3(halfWidth, localTailPosition.y, localTailPosition.z);
                }
                else
                {
                    localTailPosition = localHeadPosition;
                    localTailPosition.z = length;
                }
            } 
            
            else {
                if (Mathf.Abs(localTailPosition.y) > halfHeight)
                {
                    halfHeight = (localTailPosition.y < 0f) ? -halfHeight : halfHeight;
                    var localNormal = new Vector3(0f, localTailPosition.y - halfHeight, localTailPosition.z).normalized;
                    localTailPosition =
                        new Vector3(localTailPosition.x, halfHeight, 0f) + tailRadius * localNormal;
                }
                else if (Mathf.Abs(localTailPosition.x) > halfWidth)
                {
                    halfWidth = (localTailPosition.x < 0f) ? -halfWidth : halfWidth;
                    var localNormal = new Vector3(localTailPosition.x - halfWidth, 0f, localTailPosition.z).normalized;
                    localTailPosition = new Vector3(halfWidth, localTailPosition.y, 0f) + tailRadius * localNormal;
                }
                else
                {
                    var newLocalTailPosition = localHeadPosition;
                    if (localHeadPosition.z + length <= tailRadius)
                    {
                        // Bone is completely embedded
                        newLocalTailPosition.z += length;
                    }
                    else
                    {
                        var heightAboveRadius = localHeadPosition.z - tailRadius;
                        var projectionLength =
                            Mathf.Sqrt(length * length - heightAboveRadius * heightAboveRadius);
                        var localBoneVector = localTailPosition - localHeadPosition;
                        var projectionVector = new Vector2(localBoneVector.x, localBoneVector.y);
                        var projectionVectorLength = projectionVector.magnitude;
                        if (projectionVectorLength > 0.001f)
                        {
                            var projection = (projectionLength / projectionVectorLength) * projectionVector;
                            newLocalTailPosition = new Vector4
                            {
                                x = newLocalTailPosition.x + projection.x,
                                y = newLocalTailPosition.y + projection.y,
                                z = newLocalTailPosition.z + tailRadius,
                                w = 0f
                            };
                        }
                    }
                    localTailPosition = newLocalTailPosition;
                }
            }

            tailPosition = transform.localToWorldMatrix.MultiplyPoint3x4(localTailPosition);
            hitNormal = transform.localToWorldMatrix.MultiplyPoint3x4(Vector3.forward).normalized; 

            return true;
        }

        public static bool ResolvePanelOnAxis
        (
            Vector3 localHeadPosition,
            ref Vector3 localTailPosition,
            float localLength,
            float localTailRadius,
            Axis upAxis
        )
        {
            var zIndex = (int) upAxis;
            if (localTailPosition[zIndex] >= localTailRadius)
            {
                return false;
            }

            var newLocalTailPosition = localHeadPosition;
            if (localHeadPosition[zIndex] + localLength <= localTailRadius)
            {
                // Bone is completely embedded
                newLocalTailPosition[zIndex] += localLength;
            }
            else
            {
                var xIndex = (zIndex + 1) % (int) Axis.AxisCount;
                var yIndex = (zIndex + 2) % (int) Axis.AxisCount;

                var heightAboveRadius = localHeadPosition[zIndex] - localTailRadius;
                var projectionLength = Mathf.Sqrt(localLength * localLength - heightAboveRadius * heightAboveRadius);
                var localBoneVector = localTailPosition - localHeadPosition;
                var projectionVector = new Vector2(localBoneVector[xIndex], localBoneVector[yIndex]);
                var projectionVectorLength = projectionVector.magnitude;
                if (projectionVectorLength > 0.001f)
                {
                    var projection = (projectionLength / projectionVectorLength) * projectionVector;
                    newLocalTailPosition[xIndex] += projection.x;
                    newLocalTailPosition[yIndex] += projection.y;
                    newLocalTailPosition[zIndex] = localTailRadius;
                }
            }

            localTailPosition = newLocalTailPosition;
            return true;
        }
    }
}