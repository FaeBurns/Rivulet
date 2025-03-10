using System;
using UnityEngine;

public class ProjectionValuesTest : MonoBehaviour
{
    private void Update()
    {
        Camera mainCamera = Camera.main!;

        Matrix4x4 projectionLeft = mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        Matrix4x4 projectionRight = mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

        Matrix4x4 viewLeft = mainCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
        Matrix4x4 viewRight = mainCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

        float ipd = mainCamera.stereoSeparation;

        Debug.LogWarning("New Frame");

        Debug.Log(projectionLeft.ToString());
        Debug.Log(projectionRight.ToString());

        Debug.Log(ipd);

        Debug.Log(viewLeft.ToString());
        Debug.Log(viewRight.ToString());
    }
}