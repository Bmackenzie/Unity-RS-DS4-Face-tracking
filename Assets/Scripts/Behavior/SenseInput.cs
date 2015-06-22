using UnityEngine;
using System.Collections;

public class SenseInput : MonoBehaviour {
	public bool m_enableScenePerception=true;
	public string m_fileName="";
	public int m_width=320;
	public int m_height=240;
	public int m_fps=30;
	public PXCMSession.CoordinateSystem m_coordinateSystem=PXCMSession.CoordinateSystem.COORDINATE_SYSTEM_REAR_OPENCV;

	public delegate void OnSetupDelegate();
	public event OnSetupDelegate m_OnSetup=null;

	public delegate void OnSampleDelegate(PXCMCapture.Sample sample);
	public event OnSampleDelegate m_OnSample=null;

	public delegate void OnDataDelegate(int mid, PXCMBase module, PXCMCapture.Sample sample);
	public event OnDataDelegate m_OnData=null;

	public delegate void OnShutDownDelegate();
	public event OnShutDownDelegate m_ShutDown=null;
	
	private PXCMSenseManager m_senseManager=null;
	public PXCMSenseManager SenseManager {
		get { return m_senseManager; }
	}

	private pxcmStatus m_status=pxcmStatus.PXCM_STATUS_INIT_FAILED;
	public bool IsStreaming {
		get { return m_status>=pxcmStatus.PXCM_STATUS_NO_ERROR; }
	}

	// Callback if the OnData callback is available
	private pxcmStatus OnModuleProcessedFrames(int mid, PXCMBase module, PXCMCapture.Sample sample) {
		OnDataDelegate odd=m_OnData;
		if (odd!=null) odd(mid, module, sample);
		return pxcmStatus.PXCM_STATUS_NO_ERROR;
	}
	
	// Callback if the OnData callback is available
	private pxcmStatus OnNewSample(int mid, PXCMCapture.Sample sample) {
		OnSampleDelegate osd=m_OnSample;
		if (osd!=null) osd(sample);
		return pxcmStatus.PXCM_STATUS_NO_ERROR;
	}

	// Use this for initialization
	void Start () {
		if (m_senseManager!=null) return;

		// Create a SenseManager instance 
		m_senseManager=PXCMSenseManager.CreateInstance();
		if (m_senseManager==null) {
			Debug.Log("SenseManager Instance Failed");
			return;
		}

		// Set Coordinate System
		m_senseManager.session.SetCoordinateSystem(m_coordinateSystem);

		// Set playback if m_FileName!=null
		if (m_fileName!="") {
			m_senseManager.captureManager.SetFileName(m_fileName, false);
		}
		
		// Enable synchronized color & depth streams for continuous color display & checking scene quality.
		PXCMVideoModule.DataDesc ddesc=GetRawStreamDescs();
		m_senseManager.EnableStreams(ddesc);

		// Enable available modalities
		if (m_enableScenePerception) {
			m_senseManager.EnableScenePerception();
			m_senseManager.PauseScenePerception(true);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (m_senseManager==null) return;
		if (IsStreaming) return;

		// Give a chance of additional setups.
		OnSetupDelegate osd=m_OnSetup;
		if (osd!=null) osd();

		// Initialize the pipeline
		PXCMSenseManager.Handler handler=new PXCMSenseManager.Handler();
		handler.onNewSample=OnNewSample;
		handler.onModuleProcessedFrame=OnModuleProcessedFrames;
		m_status=m_senseManager.Init(handler);
		if (m_status<pxcmStatus.PXCM_STATUS_NO_ERROR) {
			Debug.Log ("Init Failed; " + m_status);
			return;
		}

		// Start streaming
		m_status=m_senseManager.StreamFrames(false);
		if (m_status<pxcmStatus.PXCM_STATUS_NO_ERROR)
			Debug.Log ("Stream Failed; " + m_status);
	}

	// setup to stream color & depth synchronized at widthxheightxfps
	private PXCMVideoModule.DataDesc GetRawStreamDescs() {
		PXCMVideoModule.DataDesc ddesc=new PXCMVideoModule.DataDesc();
		ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].sizeMin.width=
			ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].sizeMax.width=m_width;
		ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].frameRate.min=
			ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].frameRate.max=m_fps;
		ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH].sizeMin.height=
			ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH].sizeMax.height=m_height;
		ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH].frameRate.min=
			ddesc.streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH].frameRate.max=m_fps;
		ddesc.deviceInfo.orientation=PXCMCapture.DeviceOrientation.DEVICE_ORIENTATION_WORLD_FACING;
		return ddesc;
	}

	void OnDisable() {
		OnShutDownDelegate odd=m_ShutDown;
		if (odd!=null) odd();

		if (m_senseManager==null) return;
		m_senseManager.Dispose();
		m_senseManager=null;
	}

	void OnGUI() {
		if (IsStreaming) return;

		GUI.skin.box.hover.textColor =
			GUI.skin.box.normal.textColor =
				GUI.skin.box.active.textColor = Color.green;
		GUI.skin.box.alignment = TextAnchor.MiddleCenter;
		
		GUI.Box(new Rect(5, Screen.height-35, 100, 30), "Setup Failed");
	}
}
