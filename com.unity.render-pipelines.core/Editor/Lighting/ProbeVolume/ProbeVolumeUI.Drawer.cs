using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Rendering;

// TODO(Nicholas): deduplicate with LocalVolumetricFogUI.Drawer.cs.
namespace UnityEditor.Experimental.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        internal static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_VolumeContent,
                Drawer_BakeToolBar
            )
        );

        static void Drawer_BakeToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            if (!ProbeReferenceVolume.instance.isInitialized) return;

            Bounds bounds = new Bounds();
            bool foundABound = false;
            bool performFitting = false;
            bool performFittingOnlyOnSelection = false;

            bool ContributesToGI(Renderer renderer)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI;
                return (flags & StaticEditorFlags.ContributeGI) != 0;
            }

            void ExpandBounds(Bounds currBound)
            {
                if (!foundABound)
                {
                    bounds = currBound;
                    foundABound = true;
                }
                else
                {
                    bounds.Encapsulate(currBound);
                }
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to Scene"), EditorStyles.miniButton))
            {
                performFitting = true;
            }
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to Selection"), EditorStyles.miniButton))
            {
                performFitting = true;
                performFittingOnlyOnSelection = true;
            }

            if (performFitting)
            {
                ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);
                Undo.RecordObject(pv.transform, "Fitting Probe Volume");

                if (performFittingOnlyOnSelection)
                {
                    var transforms = Selection.transforms;
                    foreach (var transform in transforms)
                    {
                        var childrens = transform.gameObject.GetComponentsInChildren<Transform>();
                        foreach (var children in childrens)
                        {
                            Renderer childRenderer;
                            if (children.gameObject.TryGetComponent<Renderer>(out childRenderer))
                            {
                                bool childContributeGI = ContributesToGI(childRenderer) && childRenderer.gameObject.activeInHierarchy && childRenderer.enabled;

                                if (childContributeGI)
                                {
                                    ExpandBounds(childRenderer.bounds);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var renderers = UnityEngine.GameObject.FindObjectsOfType<Renderer>();

                    foreach (Renderer renderer in renderers)
                    {
                        bool contributeGI = ContributesToGI(renderer) && renderer.gameObject.activeInHierarchy && renderer.enabled;

                        if (contributeGI)
                        {
                            ExpandBounds(renderer.bounds);
                        }
                    }
                }

                pv.transform.position = bounds.center;
                float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
                Vector3 tmpClamp = (bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize));
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }
        }

        static void Drawer_ToolBar(SerializedProbeVolume serialized, Editor owner)
        {
        }

        static void Drawer_VolumeContent(SerializedProbeVolume serialized, Editor owner)
        {
            if (!ProbeReferenceVolume.instance.isInitialized)
            {
                var renderPipelineAsset = UnityEngine.Rendering.RenderPipelineManager.currentPipeline;
                if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipeline")
                {
                    EditorGUILayout.HelpBox("The probe volumes feature is disabled. The feature needs to be enabled in the HDRP Settings and on the used HDRP asset.", MessageType.Warning, wide: true);
                }
                else
                {
                    EditorGUILayout.HelpBox("The probe volumes feature is not enabled or not available on current SRP.", MessageType.Warning, wide: true);
                }

                return;
            }

            EditorGUI.BeginChangeCheck();
            if ((serialized.serializedObject.targetObject as ProbeVolume).mightNeedRebaking)
            {
                var helpBoxRect = GUILayoutUtility.GetRect(new GUIContent(Styles.s_ProbeVolumeChangedMessage, EditorGUIUtility.IconContent("Warning@2x").image), EditorStyles.helpBox);
                EditorGUI.HelpBox(helpBoxRect, Styles.s_ProbeVolumeChangedMessage, MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serialized.globalVolume, Styles.s_GlobalVolume);
            if (!serialized.globalVolume.boolValue)
                EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);

            var rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(rect, Styles.s_HighestSubdivLevel, serialized.highestSubdivisionLevelOverride);
            EditorGUI.BeginProperty(rect, Styles.s_LowestSubdivLevel, serialized.lowestSubdivisionLevelOverride);

            // Round min and max subdiv
            int maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            // it's likely we don't have a profile loaded yet.
            if (maxSubdiv < 0)
            {
                if (ProbeReferenceVolume.instance.sceneData != null)
                {
                    ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);

                    var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(pv.gameObject.scene);

                    if (profile != null)
                    {
                        ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(profile.minBrickSize, profile.maxSubdivision);
                        maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
                    }
                    else
                    {
                        maxSubdiv = 0;
                    }
                }
            }

            EditorGUILayout.LabelField("Subdivision Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            int value = serialized.highestSubdivisionLevelOverride.intValue;

            // We were initialized, but we cannot know the highest subdiv statically, so we need to resort to this.
            if (serialized.highestSubdivisionLevelOverride.intValue < 0)
                serialized.highestSubdivisionLevelOverride.intValue = maxSubdiv;

            serialized.highestSubdivisionLevelOverride.intValue = EditorGUILayout.IntSlider(Styles.s_HighestSubdivLevel, serialized.highestSubdivisionLevelOverride.intValue, 0, maxSubdiv);
            serialized.lowestSubdivisionLevelOverride.intValue = EditorGUILayout.IntSlider(Styles.s_LowestSubdivLevel, serialized.lowestSubdivisionLevelOverride.intValue, 0, maxSubdiv);
            serialized.lowestSubdivisionLevelOverride.intValue = Mathf.Min(serialized.lowestSubdivisionLevelOverride.intValue, serialized.highestSubdivisionLevelOverride.intValue);
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            int minSubdivInVolume = serialized.lowestSubdivisionLevelOverride.intValue;
            int maxSubdivInVolume = serialized.highestSubdivisionLevelOverride.intValue;
            EditorGUI.indentLevel--;

            EditorGUILayout.HelpBox($"The distance between probes will fluctuate between : {ProbeReferenceVolume.instance.GetDistanceBetweenProbes(maxSubdiv - maxSubdivInVolume)}m and {ProbeReferenceVolume.instance.GetDistanceBetweenProbes(maxSubdiv - minSubdivInVolume)}m", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 tmpClamp = serialized.size.vector3Value;
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }

            EditorGUILayout.PropertyField(serialized.objectLayerMask, Styles.s_ObjectLayerMask);

            EditorGUILayout.PropertyField(serialized.geometryDistanceOffset, Styles.s_GeometryDistanceOffset);
        }
    }
}
