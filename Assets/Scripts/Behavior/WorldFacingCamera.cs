/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2014 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using UnityEngine;

public class WorldFacingCamera : MonoBehaviour 
{
	public SenseInput m_senseInput;
	public DepthFusion m_depthFusion;

	private Camera m_camera;

    /// <summary>
    /// Main Camera position (running averaging).
    /// </summary>
    private Vector3 m_avgCamPosition;

    /// <summary>
    ///  Main Camera orientation (running averaging).
    /// </summary>
    private Quaternion m_avgCamRotation;

    /// <summary>
    /// Camera pose 6 DOF in matrix format.
    /// </summary>
    private float[] m_poseMatrix=null;

	void Start () 
    {
		// Default initial position and orientation of the camera
		m_avgCamPosition = Vector3.zero;
		m_avgCamRotation = Quaternion.identity;
		m_camera=(Camera)GetComponent("Camera");
		if (m_camera==null) m_camera=Camera.main;
	}
	
	void Update ()
	{
		if (!m_senseInput.IsStreaming) return;

		if (m_poseMatrix==null) {
			// Setup unity's projection matrix
			ConfigureProjectionMatrix(GetCameraIntrinsics());
		}

		// Update the camera's pose on every update call
		UpdateMainCameraPose();
	}

    /// <summary>
    /// Updates the main camera's pose based to the pose estimated by scene perception. 
    /// Call after tracking and meshing have been called (check execution order of scripts).
    /// </summary>
    private void UpdateMainCameraPose()
    {
		m_poseMatrix=m_depthFusion.GetCameraPose();
		if (m_poseMatrix==null) return;

        var position = new Vector3(m_poseMatrix[3], m_poseMatrix[7], m_poseMatrix[11]);
        position.y = -position.y; // Mirror position across XZ plane

        // use running average for locations to reduce jittering. 
        if (m_avgCamPosition == Vector3.zero)
        {
            m_avgCamPosition = position;
        }
        else
        {
            m_avgCamPosition = (m_avgCamPosition + position) / 2;
        }

        m_camera.transform.localPosition = m_avgCamPosition;

        Matrix4x4 rotMat = new Matrix4x4();
        rotMat.SetRow(0, new Vector4(m_poseMatrix[0], m_poseMatrix[1], m_poseMatrix[2], 0));
        rotMat.SetRow(1, new Vector4(m_poseMatrix[4], m_poseMatrix[5], m_poseMatrix[6], 0));
        rotMat.SetRow(2, new Vector4(m_poseMatrix[8], m_poseMatrix[9], m_poseMatrix[10], 0));
        rotMat.m33 = 1;

        Quaternion curCamRot = RotationMatrixToQuaterion(rotMat);
        // Mirror orientation across XZ plane
        curCamRot.x = -curCamRot.x;
        curCamRot.z = -curCamRot.z;

        //use running "average" for rotations to reduce jittering
        if (m_avgCamRotation == Quaternion.identity)
        {
            m_avgCamRotation = curCamRot;
        }
        else
        {
            m_avgCamRotation = Quaternion.Lerp(m_avgCamRotation, curCamRot, 0.75f);
        }

        m_camera.transform.localRotation = m_avgCamRotation;
    }

    /// <summary>
    /// Configures the projection matrix for unity.
    /// </summary>
    /// <param name="camIntrinsics">Camera instrinsics.</param>
    private void ConfigureProjectionMatrix(CameraIntrinsics camIntrinsics)
    {
        Matrix4x4 unityCameraParamsMatrix = SetUnityCameraParamsMatrix(camIntrinsics);
        Matrix4x4 othrographicMatrix = CalculateOrthographicMatrix(camIntrinsics);
        Matrix4x4 cameraProjectionMatrix = new Matrix4x4();

        cameraProjectionMatrix = othrographicMatrix * unityCameraParamsMatrix;
        m_camera.projectionMatrix = cameraProjectionMatrix;
    }

    /// <summary>
    /// Sets unity's camera parameters matrix based on focal lengths (fx, fy) and principle points (u, v) 
    /// that will be used while computing the projection matrix.
    /// </summary>
    /// <param name="cameraParams">Instrinsics of the camera.</param>
    /// <returns>Camera intrinsics matrix.</returns>
    private Matrix4x4 SetUnityCameraParamsMatrix(CameraIntrinsics cameraParams)
    {
        Matrix4x4 unityCamParamsMat = new Matrix4x4();

        unityCamParamsMat.m00 = cameraParams.focalLength.x;//negative for mirroring
        unityCamParamsMat.m02 = -cameraParams.principalPoint.x;
        unityCamParamsMat.m11 = cameraParams.focalLength.y; //negative for flipping
        unityCamParamsMat.m12 = -cameraParams.principalPoint.y;
        unityCamParamsMat.m22 = (m_camera.nearClipPlane + m_camera.farClipPlane);
        unityCamParamsMat.m23 = (m_camera.nearClipPlane * m_camera.farClipPlane);
        unityCamParamsMat.m32 = -1f;

        return unityCamParamsMat;
    }

    /// <summary>
    /// Calculates an orthographic matrix using camera intrinsincs to be used while computing the projection matrix.
    /// </summary>
    /// <param name="cameraIntrinsics">Instrinsics of the camera.</param>
    /// <returns>Orthographic matrix computed using camera intrinsincs.</returns>
    private Matrix4x4 CalculateOrthographicMatrix(CameraIntrinsics cameraIntrinsics)
    {
        int L = 0;
        int R = (int)cameraIntrinsics.size.width;
        int B = 0;
        int T = (int)cameraIntrinsics.size.height;

        Matrix4x4 orthographicMatrix = new Matrix4x4();
        orthographicMatrix.m00 = 2.0f / (R - L);
        orthographicMatrix.m03 = -(R + L) / (R - L);
        orthographicMatrix.m11 = 2.0f / (T - B);
        orthographicMatrix.m13 = -(T + B) / (T - B);
        orthographicMatrix.m22 = -2.0f / (m_camera.farClipPlane - m_camera.nearClipPlane);
        orthographicMatrix.m23 = -(m_camera.farClipPlane + m_camera.nearClipPlane) /
                        (m_camera.farClipPlane - m_camera.nearClipPlane);
        orthographicMatrix.m33 = 1.0f;

        return orthographicMatrix;
    }
    
    /// <summary>
    /// Converts rotation matrix to quaternion.
    /// </summary>
    /// <param name="rotMat">Input rotation matrix.</param>
    /// <returns>Rotation matrix converted to quaternion.</returns>
    private Quaternion RotationMatrixToQuaterion(Matrix4x4 rotMat)
    {
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + rotMat[0, 0] + rotMat[1, 1] + rotMat[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + rotMat[0, 0] - rotMat[1, 1] - rotMat[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - rotMat[0, 0] + rotMat[1, 1] - rotMat[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - rotMat[0, 0] - rotMat[1, 1] + rotMat[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (rotMat[2, 1] - rotMat[1, 2]));
        q.y *= Mathf.Sign(q.y * (rotMat[0, 2] - rotMat[2, 0]));
        q.z *= Mathf.Sign(q.z * (rotMat[1, 0] - rotMat[0, 1]));
        return q;
    }

	private CameraIntrinsics GetCameraIntrinsics() {
		if (!m_senseInput.IsStreaming) return null;

		PXCMSenseManager sm=m_senseInput.SenseManager;
		PXCMCaptureManager cm=sm.captureManager;
		PXCMCapture.Device device=cm.device;

		CameraIntrinsics data=new CameraIntrinsics();
		data.size=cm.QueryImageSize(PXCMCapture.StreamType.STREAM_TYPE_COLOR);
		data.focalLength=device.QueryColorFocalLength();
		data.principalPoint=device.QueryColorPrincipalPoint();
		return data;
	}
}