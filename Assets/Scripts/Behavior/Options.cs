using UnityEngine;
using System;
using System.Collections;

public class Options : MonoBehaviour {
	public SenseInput  m_senseInput;
	public DepthFusion m_depthFusion;

	/// <summary>
	/// Width of the gui buttons.
	/// </summary>
	private const int GUI_BUTTON_WIDTH = 110;

	/// <summary>
	/// Bool to flag if the start button has been clicked.
	/// </summary>
	private bool m_startButtonIsClicked = false;

	/// <summary>
	/// Height of the gui buttons.
	/// </summary>
	private const int GUI_BUTTON_HEIGHT = 35;

	/// <summary>
	/// String to display if meshing is enabled/disabled.
	/// </summary>
	private string m_strMeshing="Meshing";

	/// <summary>
	/// String to display if extended reconstruction is enabled/disabled.
	/// </summary>
	private string m_strReconstruction= "Reconstruction";

	/// <summary>
	/// String to display if tracking is enabled/disabled.
	/// </summary>
	private string m_strTracking="Tracking";

	private float m_sceneQuality=0;
	public float m_minSceneQuality = 0.5f;
	public bool SceneQualityOK {
		get { return m_sceneQuality>=m_minSceneQuality; }
	}
	
	// Use this for initialization
	void Start () {
		m_senseInput.m_OnSample+=CheckSceneQuality;
		m_startButtonIsClicked=false;	
	}
	
	// Update is called once per frame
	void Update () {
	}

	void OnGUI()
	{
		int groupXOffset = Screen.width - GUI_BUTTON_WIDTH - 10;
		int guiGroupWidth = GUI_BUTTON_WIDTH + 10;
		
		GUI.skin.box.hover.textColor =
			GUI.skin.box.normal.textColor =
				GUI.skin.box.active.textColor = Color.white;
		GUI.skin.box.alignment = TextAnchor.UpperCenter;
		
		GUI.skin.button.active.textColor = Color.green;
		
		// Text msg if start button has been clicked
		if (m_startButtonIsClicked)
		{
			int guiGroupHeight = 265;
			GUI.BeginGroup(new Rect(groupXOffset - 5, (Screen.height - guiGroupHeight) / 2, guiGroupWidth, guiGroupHeight));
			GUI.Box(new Rect(0, 0, guiGroupWidth, guiGroupHeight), "Controls");
			
			// Tracking button
			if (m_depthFusion.TrackingEnabled)
			{
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor =
						GUI.skin.button.active.textColor = Color.green;
			}
			else
			{
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor = Color.white;
			}
			if(GUI.Button(new Rect(5, 25, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), m_strTracking))
				m_depthFusion.ToggleTracking();
			
			// Reconstruction button
			if (m_depthFusion.ReconstructionEnabled)
			{
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor =
						GUI.skin.button.active.textColor = Color.green;
			}
			else
			{
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor = Color.white;
			} 
			if (GUI.Button(new Rect(5, 65, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), m_strReconstruction))
				m_depthFusion.ToggleReconstruction();
			
			// Meshing button
			if (m_depthFusion.MeshingEnabled)
			{
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor =
						GUI.skin.button.active.textColor = Color.green;
			}
			else
			{
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor = Color.white;
			} 
			if (GUI.Button(new Rect(5, 105, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), m_strMeshing))
				m_depthFusion.ToggleMeshing();
			
			// Save mesh button
			if (m_depthFusion.m_meshFileName!="") {
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor = Color.white;
			} else {
				GUI.skin.button.hover.textColor =
					GUI.skin.button.normal.textColor = Color.gray;
			}
			if(GUI.Button(new Rect(5, 145, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), "Save Mesh")) {
				m_depthFusion.SaveMesh();
			}

			// Reset button
			GUI.skin.button.hover.textColor =
				GUI.skin.button.normal.textColor = Color.white;
			if(GUI.Button(new Rect(5, 185, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), "Reset")) {
				m_depthFusion.Reset();
				m_startButtonIsClicked=false;
			}
			
			// Quit button
			if(GUI.Button(new Rect(5, 225, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), "Quit"))
				Application.Quit();
			
			GUI.EndGroup();
		}
		
		// If start button hasn't been clicked
		else
		{
			// Not yet a start button, let's enable tracking. This will give us the chance of evaluating scene quality.
			if (m_depthFusion.ReconstructionEnabled) m_depthFusion.ToggleReconstruction();
			if (m_depthFusion.MeshingEnabled) m_depthFusion.ToggleMeshing();
			if (m_depthFusion.TrackingEnabled) m_depthFusion.ToggleTracking();

			int guiGroupHeight = 105;
			GUI.skin.button.hover.textColor = 
				GUI.skin.button.normal.textColor = 
					GUI.skin.button.active.textColor = Color.white;
			
			GUI.BeginGroup(new Rect(groupXOffset - 5, (Screen.height - guiGroupHeight) / 2, guiGroupWidth, guiGroupHeight));
			GUI.Box(new Rect(0, 0, guiGroupWidth, guiGroupHeight), "Controls");
			
			if (SceneQualityOK) {
				// Start button
				if (GUI.Button(new Rect(5, 25, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), "Start")) {
					m_depthFusion.ToggleTracking();
					m_depthFusion.ToggleReconstruction();
					m_depthFusion.ToggleMeshing();
					m_startButtonIsClicked = true;
				}
			}
			
			// Quit button
			if(GUI.Button(new Rect(5, 65, GUI_BUTTON_WIDTH, GUI_BUTTON_HEIGHT), "Quit"))
				Application.Quit();
			
			GUI.EndGroup();

			// Show scene quality
			GUI.skin.box.hover.textColor =
				GUI.skin.box.normal.textColor =
					GUI.skin.box.active.textColor = Color.green;
			GUI.skin.box.alignment = TextAnchor.MiddleCenter;
			GUI.Box(new Rect(5, Screen.height-35, 40, 30), Math.Round(m_sceneQuality, 2).ToString());
		}
	}

	private void CheckSceneQuality(PXCMCapture.Sample sample) {
		if (!m_senseInput.IsStreaming) return;
		if (m_startButtonIsClicked) return;
		m_sceneQuality=m_depthFusion.CheckSceneQuality(sample);
	}
}
