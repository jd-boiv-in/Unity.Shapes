using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace JD.Shapes.Editor
{
    // Fix when managed stripping is high
    public class ShapeBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"Shapes Build Processor");
            
            // Ensure shaders are included and instancing variants as well
            var asset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/GraphicsSettings.asset");
            if (asset == null)
            {
                Debug.LogError($"Cannot load GraphicsSettings.asset");
                return;
            }
            
            var serObj = new SerializedObject(asset);
            serObj.UpdateIfRequiredOrScript();

            var includedShadersProp = serObj.FindProperty("m_AlwaysIncludedShaders");
            if (includedShadersProp == null)
            {
                Debug.LogError($"Cannot find m_AlwaysIncludedShaders property");
                return;
            }
            
            var instancingStrippingProp = serObj.FindProperty("m_InstancingStripping");
            if (instancingStrippingProp == null)
            {
                Debug.LogError($"Cannot find m_InstancingStripping property");
                return;
            }

            instancingStrippingProp.intValue = 2;

            var shaders = new[]
            {
                "Hidden/Shapes/Circle",
                "Hidden/Shapes/Label",
                "Hidden/Shapes/Label Billboard",
                "Hidden/Shapes/Line",
                "Hidden/Shapes/Polygon",
                "Hidden/Shapes/Rect",
                "Hidden/Shapes/ArrowHead",
                "Hidden/Shapes/Debug",
            };
            
            foreach (var name in shaders)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogError($"Cannot find shader with name");
                    return;
                }

                AddShader(includedShadersProp, shader);
            }
            
            serObj.ApplyModifiedPropertiesWithoutUndo();
        }
        
        private void AddShader(SerializedProperty includedShadersProp, Shader shader)
        {
            // Add shader if not present
            for (int i = 0, count = includedShadersProp.arraySize; i < count; ++i)
            {
                var element = includedShadersProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == shader)
                {
                    return; // Shader added already
                }
            }

            includedShadersProp.arraySize++;
            var shaderProp = includedShadersProp.GetArrayElementAtIndex(includedShadersProp.arraySize - 1);
            shaderProp.objectReferenceValue = shader;
        }
    }
}