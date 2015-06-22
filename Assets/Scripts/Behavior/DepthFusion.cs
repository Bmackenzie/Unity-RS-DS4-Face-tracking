/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2015 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

public class DepthFusion : MonoBehaviour
{
	public SenseInput m_senseInput;
	public GameObject m_meshPrefab;
	
	public PXCMScenePerception.VoxelResolution m_volxelResolution=PXCMScenePerception.VoxelResolution.LOW_RESOLUTION;
	public string m_meshFileName="";
	public int m_meshUpdateFreqInFrames=60;			// update mesh every specified frames.
	public int m_meshAddFreqInObjects=50;       // render the specified number of mesh objects every update.
	public int m_meshDeleteFreqInObjects=50;       // render the specified number of mesh objects every update.
	public float m_worldScale=1.0f;
	public Advanced advanced=new Advanced();

	[Serializable]
	public class Advanced {
		public float m_maxDiffThreshold=0.03f;
		public float m_avgDiffThreshold=0.005f;
		public bool  m_fillHoles=false;
	}

	private PXCMScenePerception m_scenePerception=null;

	private bool m_trackingEnabled=false;
	public bool TrackingEnabled {
		get { return m_trackingEnabled; }
	}

	private bool m_meshingEnabled=false;
	public bool MeshingEnabled {
		get { return m_meshingEnabled; }
	}

	private bool m_reconstructionEnabled=false;
	public bool ReconstructionEnabled {
		get { return m_reconstructionEnabled; }
	}

	private Dictionary<int, GameObject> m_gameObjects=new Dictionary<int, GameObject>();
	private Dictionary<int, KeyValuePair<Vector3[],int[]>> m_meshes=new Dictionary<int, KeyValuePair<Vector3[], int[]>> ();
	private LinkedList<int> m_renderQueue=new LinkedList<int>();
	private LinkedList<int> m_destructionQueue=new LinkedList<int>();

	/// <summary>
	/// Static flag to hide/show the mesh.
	/// </summary>
	internal static bool m_meshIsVisible=true;

	private Thread m_meshingThread=null;
	private bool   m_meshingThreadRunning=false;
	private object m_meshingLock=new object();
	private Thread m_savingThread=null;

	/// <summary>
	/// Parent mesh for all the mesh pieces determined post-meshing.
	/// </summary>
	private GameObject m_meshParent;
	private int m_nframes=0;

	PXCMBlockMeshingData m_bmui = null;

	void Start ()
	{
		m_senseInput.m_OnSetup+=Setup;
		m_senseInput.m_ShutDown+=ShutDown;

		// Initialize the unity objects for the meshes
		m_meshParent = new GameObject("Parent Mesh");

		// Set the appropriate flags and set strings to empty
		m_meshIsVisible = true;
	}

	void Update()
	{
		if (m_scenePerception==null) return;
		
		PXCMScenePerception.TrackingAccuracy accuracy=m_scenePerception.QueryTrackingAccuracy();
		m_meshIsVisible=(accuracy!=PXCMScenePerception.TrackingAccuracy.FAILED);
		
		if (!m_meshingEnabled) return;
		if (m_meshingThreadRunning) return;
		m_nframes++;

		if ((m_nframes%(int)m_meshUpdateFreqInFrames)==0) {
			if (m_scenePerception.IsReconstructionUpdated()) {
				m_meshingThread=new Thread(MeshingThread);
				m_meshingThreadRunning=true;
				m_meshingThread.Start();
			}
		}

		// Update meshes
		RenderMesh();
	}
	
	public float CheckSceneQuality(PXCMCapture.Sample sample) {
		if (m_scenePerception==null) return 0;
		return m_scenePerception.CheckSceneQuality(sample);
	}

	private void Setup() {
		m_scenePerception=m_senseInput.SenseManager.QueryScenePerception();
		m_scenePerception.SetMeshingThresholds(0,0);

		float volumeSize=4.0f;
		m_scenePerception.SetVoxelResolution(m_volxelResolution);
		switch (m_volxelResolution) {
		case PXCMScenePerception.VoxelResolution.LOW_RESOLUTION:
			volumeSize=4.0f;
			break;
		case PXCMScenePerception.VoxelResolution.MED_RESOLUTION:
			volumeSize=2.0f;
			break;
		case PXCMScenePerception.VoxelResolution.HIGH_RESOLUTION:
			volumeSize=1.0f;
			break;
		}

		// Create the camera's default pose and translation matrices
		float[] camDefaultPoseMatrix = new float[]
		{
			1.0f,0.0f,0.0f, volumeSize/2.0f,
			0.0f,1.0f,0.0f, volumeSize/2.0f,
			0.0f,0.0f,1.0f,0.0f
		};
		m_scenePerception.SetInitialPose(camDefaultPoseMatrix);

		m_bmui = m_scenePerception.CreatePXCMBlockMeshingData(-1,-1,-1,false);

	}

	private void ShutDown() {
		Reset (true);
        if (m_bmui != null)
            m_bmui.Dispose(); 
	}

	public void Reset() {
		Reset (false);
	}

	/// <summary>
	/// Performs a reset on the depth fusion module.
	/// </summary>
	private void Reset(bool shutdown)
	{
		m_meshingEnabled=false;

		if (m_meshingThread!=null) {
			m_meshingThread.Join();
			m_meshingThread=null;
		}

		if (m_savingThread!=null) {
			m_savingThread.Join();
			m_savingThread=null;
		}

		lock(m_meshingLock) {
			m_bmui.Reset();
			m_meshes.Clear();
			m_renderQueue.Clear();
			m_destructionQueue.Clear();

			m_gameObjects.Clear();
			Destroy(m_meshParent);
			m_meshParent=shutdown?null:new GameObject("Parent Mesh");
		}

		if (m_scenePerception!=null) {
			pxcmStatus sts= m_scenePerception.Reset();
			Debug.Log ("Reset Status: " + sts);
			m_scenePerception.SetMeshingThresholds(0,0);
		}
	}
	
	/// <summary>
	/// Toggles the property to perform meshing.
	/// </summary>
	public void ToggleMeshing()
	{
		m_meshingEnabled = !m_meshingEnabled;
	}
	
	/// <summary>
	/// Toggles the property to perform reconstruction.
	/// </summary>
	public void ToggleReconstruction()
	{
		if (m_scenePerception==null) return;
		m_reconstructionEnabled=!m_reconstructionEnabled;
		m_scenePerception.EnableSceneReconstruction(m_reconstructionEnabled);
	}
	
	/// <summary>
	/// Toggles the property to perform tracking and updates the status string.
	/// </summary>
	public void ToggleTracking()
	{
		if (m_scenePerception==null) return;
		m_trackingEnabled=!m_trackingEnabled;
		m_senseInput.SenseManager.PauseScenePerception(!m_trackingEnabled);
	}
	
	private void MeshingThread() {


		Debug.Log ("MeshThread started");
		lock(m_meshingLock) {

			pxcmStatus sts=m_scenePerception.DoMeshingUpdate(m_bmui, advanced.m_fillHoles);
			m_scenePerception.SetMeshingThresholds(advanced.m_maxDiffThreshold, advanced.m_avgDiffThreshold);

			if (sts>=pxcmStatus.PXCM_STATUS_NO_ERROR) {
				int[] faces=m_bmui.QueryFaces();
				float[] vertices=m_bmui.QueryVertices();
				PXCMBlockMeshingData.PXCMBlockMesh[] meshes=m_bmui.QueryBlockMeshes();
				if (meshes != null) {
					for(int meshIndex=0; meshIndex< m_bmui.QueryNumberOfBlockMeshes(); ++meshIndex){
						PXCMBlockMeshingData.PXCMBlockMesh mesh = meshes[meshIndex];
						if ((mesh.numVertices <= 0) || (mesh.numFaces <= 0)) {
							m_destructionQueue.AddLast(mesh.meshId);
							continue;
						}
	
						// face indices relative to vertex buffer
						int[] sub_meshFaces = new int[mesh.numFaces * 3];
						for (int k = 0; k < sub_meshFaces.Length; k++)
							sub_meshFaces[k]=faces[mesh.faceStartIndex + k] - mesh.vertexStartIndex / 4;
						
						Vector3[] sub_meshVertices = new Vector3[mesh.numVertices];
						for (int i=0,j=mesh.vertexStartIndex;i<sub_meshVertices.Length;i++,j+=4)
							sub_meshVertices[i]=new Vector3(vertices[j]*m_worldScale, -vertices[j+1]*m_worldScale, vertices[j+2]*m_worldScale);
	
						// Add to the rendering queue if new.
						if (!m_meshes.ContainsKey(mesh.meshId))
							m_renderQueue.AddLast(mesh.meshId);
						m_meshes[mesh.meshId]=new KeyValuePair<Vector3[],int[]>(sub_meshVertices, sub_meshFaces);
					}
				}
			}
		}
		m_meshingThreadRunning=false;
	}

	/// <summary>
	/// Logic to display the mesh pieces in the scene.
	/// </summary>
	private void RenderMesh()
	{
		if (Monitor.TryEnter(m_meshingLock)) {
			try {
				// add the new meshes to the game object collection
				for (int k=0;k<m_meshAddFreqInObjects && m_renderQueue.Count>0;k++) {
					int meshId=m_renderQueue.First.Value;

					GameObject gameObjInstance=(GameObject)Instantiate(m_meshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
					gameObjInstance.transform.parent = m_meshParent.transform;
					gameObjInstance.GetComponent<DynamicMesh>().SetData(meshId, m_meshes[meshId].Key, m_meshes[meshId].Value);

					if (m_gameObjects.ContainsKey(meshId))
						Destroy (m_gameObjects[meshId]);
					m_gameObjects[meshId]=gameObjInstance;

					m_meshes.Remove(meshId);
					m_renderQueue.RemoveFirst();
				}

				// Destruct outdated meshes
				for (int k=0;k<m_meshDeleteFreqInObjects && m_destructionQueue.Count>0;k++) {
					int meshId=m_destructionQueue.First.Value;
					if (m_gameObjects.ContainsKey(meshId))
						Destroy (m_gameObjects[meshId]);
					m_gameObjects.Remove(meshId);
					m_destructionQueue.RemoveFirst();
				}
			} catch {
			}
			Monitor.Exit(m_meshingLock);
		}
	}

	public void SaveMesh() {
		if (m_scenePerception==null) return;

		if (m_savingThread!=null)
			m_savingThread.Join();

		m_savingThread=new Thread(SavingMeshThread);
		m_savingThread.Start ();
	}

	private void SavingMeshThread() {
		if (m_scenePerception==null) return;
		m_scenePerception.SaveMesh(m_meshFileName);
	}

	public float[] GetCameraPose() {
		if (m_scenePerception==null) return null;

		float[] pose=new float[12];
		m_scenePerception.GetCameraPose(pose);
		return pose;
	}

	void OnGUI()
	{
		if (!m_senseInput.IsStreaming) return;
		if (!ReconstructionEnabled || m_scenePerception==null) return;

		GUI.skin.box.hover.textColor =
			GUI.skin.box.normal.textColor =
				GUI.skin.box.active.textColor = Color.green;
		GUI.skin.box.alignment = TextAnchor.MiddleCenter;
		GUI.Box(new Rect(5, Screen.height-35, 80, 30), m_scenePerception.QueryTrackingAccuracy().ToString());
	}
}