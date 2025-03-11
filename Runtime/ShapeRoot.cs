using System;
using UnityEngine;

namespace JD.Shapes
{
    public class ShapeRoot : MonoBehaviour
    {
        private void OnRenderObject()
        {
            ShapeCommon.LineMatrix = transform.localToWorldMatrix;
            Shape.OnRender();
        }

        private void Update()
        {
            Shape.OnUpdate();
        }
    }
}