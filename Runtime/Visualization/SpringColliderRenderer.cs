using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{

    [ExecuteInEditMode]
    public class SpringColliderRenderer : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool showInRuntime;
        public bool showInScene;
        public Color colliderColor = Color.magenta;
        
        private List<SpringCollider> m_colliders;
        private Mesh m_quad; 
        private Mesh m_cylinder; 

        private void OnEnable()
        {
            m_colliders = new List<SpringCollider>();
            m_quad = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
            m_cylinder = Resources.GetBuiltinResource(typeof(Mesh), "Cylinder.fbx") as Mesh;
            GetComponentsInChildren(m_colliders);
        }

        // Update is called once per frame
        void OnDrawGizmos()
        {
            if (showInRuntime)
            {
                if (Application.isEditor)
                {
                    GetComponentsInChildren(m_colliders);
                }
                Gizmos.color = colliderColor;
                
                foreach (var c in m_colliders)
                {
                    var pos = c.transform.position;
                    switch (c.type)
                    {
                        case ColliderType.Sphere:
                            Gizmos.DrawWireSphere(pos, c.radius);
                            break;
                        case ColliderType.Panel:
                            Gizmos.DrawWireMesh(m_quad, pos, c.transform.rotation, new Vector3(c.width, c.height, 1f) );
                            break;
                        case ColliderType.Capsule:
                            var shift = Mathf.Max(0, c.height - c.radius);
                            var posUp = c.transform.TransformPoint(new Vector3(0f, shift, 0f));
                            var posDown = c.transform.TransformPoint(new Vector3(0f, -shift, 0f));
                            var bodyHeight = shift;
                            Gizmos.DrawWireSphere(posUp, c.radius);
                            Gizmos.DrawWireMesh(m_cylinder, pos, c.transform.rotation, new Vector3(c.radius, bodyHeight, c.radius) );
                            Gizmos.DrawWireSphere(posDown, c.radius);
                            break;
                    }
                }
            }
        }
#endif
    }
}