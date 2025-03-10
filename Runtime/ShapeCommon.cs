﻿using UnityEngine;

namespace JD.Shapes
{
    public static class ShapeCommon
    {
        public static float Alpha = 0.2f;
        
        public static float TextSize = 1.0f;
        public static Quaternion TextRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        
        public static float LineAlpha = 0.5f;
        public static Vector3 LineRotation = new Vector3(0, 0, 1);
        public static Matrix4x4 LineMatrix = Matrix4x4.identity;
        
        public static Vector3 CircleRotation = new Vector3(0, 0, 1);
        public static float CircleBorderWidth = 0.025f;
        
        public static Quaternion RectRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        public static float RectBorderWidth = 0.0333333333333333f;
    }
}