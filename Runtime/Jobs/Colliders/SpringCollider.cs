﻿using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    // Authoring component
    public class SpringCollider : MonoBehaviour
    {
        public SpringCollisionLayerMask layer;
        public ColliderType type;
        public float radius;
        public float width;
        public float height;
    }
}