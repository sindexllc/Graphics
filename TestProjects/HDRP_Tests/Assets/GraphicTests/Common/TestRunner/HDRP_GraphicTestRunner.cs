﻿using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using EditorSceneManagement = UnityEditor.SceneManagement;
#endif

public class HDRP_GraphicTestRunner
{
    [UnityTest, Category("HDRP Graphic Tests")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i=0 ; i<5 ; ++i)
            yield return null;

        // Load the test settings
        var settings = GameObject.FindObjectOfType<HDRP_TestSettings>();

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        Time.captureFramerate = settings.captureFramerate;

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        for (int i=0 ; i<settings.waitFrames ; ++i)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
    }

// Old code to auto generate the lightmaps base on scene asset tag ... this should no be needed anymore apparently. The class need to herit from IPrebuildSetup
/*
    public void Setup()
    {
#if UNITY_EDITOR

        // Search for "InitTestSceneXXXXXXXX" generated by test runner and save the path in the EditorPrefs
        for (int i=0 ; i<EditorSceneManagement.EditorSceneManager.sceneCount ; ++i)
        {
            Scene scene = EditorSceneManagement.EditorSceneManager.GetSceneAt(i);
            if (scene.name.StartsWith("InitTestScene"))
            {
                EditorPrefs.SetString("InitTestScene", scene.path);
                break;
            }
        }

        string scenesWithAutoLightMap = "";

        // For each scene in the build settings, force build of the lightmaps if it has "DoLightmap" label.
        foreach( EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            var labels = new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(sceneAsset));

            if ( labels.Contains("DoLightmap") )
            {
                EditorSceneManagement.EditorSceneManager.OpenScene(scene.path, EditorSceneManagement.OpenSceneMode.Single);
                
                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                EditorSceneManagement.EditorSceneManager.SaveOpenScenes();

                Lightmapping.Clear();
                Lightmapping.Bake();

                scenesWithAutoLightMap += scene.path + ";";

                EditorSceneManagement.EditorSceneManager.SaveOpenScenes();

                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Iterative;
                EditorSceneManagement.EditorSceneManager.SaveOpenScenes();

                EditorSceneManagement.EditorSceneManager.NewScene(EditorSceneManagement.NewSceneSetup.EmptyScene);
            }
        }

        EditorPrefs.SetString("ScenesWithAutoLightMap", scenesWithAutoLightMap);

        // Re-open testrunner scene
        string initTestSceneString = EditorPrefs.GetString("InitTestScene");
        if (!string.IsNullOrEmpty(initTestSceneString))
        {
            EditorSceneManagement.EditorSceneManager.OpenScene(initTestSceneString, EditorSceneManagement.OpenSceneMode.Single);
        }
#endif
    }
*/

#if UNITY_EDITOR

    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif

}