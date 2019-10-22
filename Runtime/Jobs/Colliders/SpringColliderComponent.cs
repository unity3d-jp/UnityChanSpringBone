using System;
using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    public struct Intersection
    {
        public Vector3 origin;
        public Vector3 upVector;
        public float radius;
    }
    
    public struct SpringCollisionLayerMask
    {
        public int value;

        public static implicit operator int(SpringCollisionLayerMask mask)
        {
            return mask.value;
        }

        public static implicit operator SpringCollisionLayerMask(int intVal)
        {
            SpringCollisionLayerMask layerMask;
            layerMask.value = intVal;
            return layerMask;
        }
    }

    public enum ColliderType
    {
        Sphere,
        Panel,
        Capsule
    }
    
    [Serializable]
    public struct SpringColliderComponent
    {
        public SpringCollisionLayerMask layer;
        public ColliderType type;
        public float radius;
        public float width;
        public float height;
    }
}