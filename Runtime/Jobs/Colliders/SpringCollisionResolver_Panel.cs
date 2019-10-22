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
            var localTailPosition = transform.localToWorldMatrix * tailPosition;
//            var localTailRadius = (transform.localToWorldMatrix * new Vector3(tailRadius, 0f, 0f)).magnitude;
            var localTailRadius = tailRadius;
            if (localTailPosition.z >= localTailRadius)
            {
                return false;
            }

            var localHeadPosition = transform.worldToLocalMatrix * (headPosition);
//            var localLength = (transform.worldToLocalMatrix * new Vector3(length, 0f, 0f)).magnitude;
            var localLength = length;

            var halfWidth = 0.5f * panel.width;
            var halfHeight = 0.5f * panel.height;
            var adjustedWidth = halfWidth + localTailRadius;
            var adjustedHeight = halfHeight + localTailRadius;

            var tailOutOfBounds = Mathf.Abs(localTailPosition.y) >= adjustedHeight
                                  || Mathf.Abs(localTailPosition.x) >= adjustedWidth;

            if (tailOutOfBounds)
            {
                return false;
            }

            // Check edges
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
                    localTailPosition.z = localTailRadius;
                }
            } else {
                if (Mathf.Abs(localTailPosition.y) > halfHeight)
                {
                    halfHeight = (localTailPosition.y < 0f) ? -halfHeight : halfHeight;
                    var localNormal = new Vector3(0f, localTailPosition.y - halfHeight, localTailPosition.z).normalized;
                    localTailPosition =
                        new Vector3(localTailPosition.x, halfHeight, 0f) + localTailRadius * localNormal;
                }
                else if (Mathf.Abs(localTailPosition.x) > halfWidth)
                {
                    halfWidth = (localTailPosition.x < 0f) ? -halfWidth : halfWidth;
                    var localNormal = new Vector3(localTailPosition.x - halfWidth, 0f, localTailPosition.z).normalized;
                    localTailPosition = new Vector3(halfWidth, localTailPosition.y, 0f) + localTailRadius * localNormal;
                }
                else
                {
                    const int xIndex = (int) Axis.X;
                    const int yIndex = (int) Axis.Y;
                    const int zIndex = (int) Axis.Z;
                    
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
                        var heightAboveRadius = localHeadPosition[zIndex] - localTailRadius;
                        var projectionLength =
                            Mathf.Sqrt(localLength * localLength - heightAboveRadius * heightAboveRadius);
                        var localBoneVector = localTailPosition - localHeadPosition;
                        var projectionVector = new Vector2(localBoneVector[xIndex], localBoneVector[yIndex]);
                        var projectionVectorLength = projectionVector.magnitude;
                        if (projectionVectorLength > 0.001f)
                        {
                            var projection = (projectionLength / projectionVectorLength) * projectionVector;
                            newLocalTailPosition = new Vector4
                            {
                                x = newLocalTailPosition.x + projection.x,
                                y = newLocalTailPosition.y + projection.y,
                                z = newLocalTailPosition.z + localTailRadius,
                                w = 0f
                            };
                        }
                    }
                    localTailPosition = newLocalTailPosition;
                }
            }

            tailPosition = transform.localToWorldMatrix * (localTailPosition);
            //hitNormal = transform.forward.normalized;
            hitNormal = (transform.localToWorldMatrix * Vector3.forward).normalized; 

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