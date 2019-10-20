using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    [System.Serializable]
    public struct AngleLimitComponent
    {
        public bool active;
        public float min;
        public float max;

        private static float ComputeFalloff(float value, float range)
        {
            const float Threshold = 0.0001f;
            if (Mathf.Abs(range) <= Threshold) { return 0f; }

            var normalizedValue = value / range;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            return Mathf.Min(normalizedValue, Mathf.Sqrt(normalizedValue));
        }

        // Returns true if exceeded bounds
        public void ConstrainVector
        (
            Vector3 basisSide,
            Vector3 basisUp,
            Vector3 basisForward,
            float springStrength,
            float deltaTime,
            ref Vector3 vector
        )
        {
            var upProjection = Vector3.Project(vector, basisUp);
            var projection = vector - upProjection;
            var projectionMagnitude = projection.magnitude;
            var originalSine = Vector3.Dot(projection / projectionMagnitude, basisSide);
            // The above math might have a bit of floating point error 
            // so clamp the sine value into a valid range so we don't get NaN later
            originalSine = Mathf.Clamp(originalSine, -1f, 1f);

            // Use soft limits based on Hooke's Law to reduce jitter,
            // then apply hard limits
            var newAngle = Mathf.Rad2Deg * Mathf.Asin(originalSine);
            var acceleration = -newAngle * springStrength;
            newAngle += acceleration * deltaTime * deltaTime;

            var minAngle = min;
            var maxAngle = max;
            var preClampAngle = newAngle;
            newAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);

            // Apply falloff
            var curveLimit = (newAngle < 0f) ? minAngle : maxAngle;
            newAngle = ComputeFalloff(newAngle, curveLimit) * curveLimit;

            var radians = Mathf.Deg2Rad * newAngle;
            var newProjection = Mathf.Sin(radians) * basisSide + Mathf.Cos(radians) * basisForward;
            newProjection *= projectionMagnitude;
            vector = newProjection + upProjection;
        }
    }
}