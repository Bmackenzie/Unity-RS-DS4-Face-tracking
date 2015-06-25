using UnityEngine;
using System.Collections;
using System;

public class FaceTracker : MonoBehaviour {

	public SenseInput m_senseInput;
	private bool shuttingDown = false;

	private PXCMImage m_sample;

	private int numExpressions = 20;
	private int numDetections = 5;
	private int numLandmarks = 180;
	private int numPoses = 20;

	// Use this for initialization
	void Start () {
		m_senseInput.m_OnSample+=OnSample;
		m_senseInput.m_ShutDown+=OnShutdown;
		m_senseInput.SenseManager.EnableFace ();

		if (m_senseInput.SenseManagern == null){
			throw new Exception("PXCMSenseManager null");
		}
		
		if (m_senseInput.SenseManager.captureManager == null){
			throw new Exception("PXCMCaptureManager null");
		}
		
		if (m_senseInput.m_height != null && m_senseInput.m_width != null){
			var streamProfile = new PXCMCapture.Device.StreamProfileSet{
				color = 
				{
					frameRate = m_senseInput.m_fps,
					imageInfo = 
					{
						//Formet????
						height = m_senseInput.m_height,
						width = m_senseInput.m_width
					}
				}
			};
			
			// DO I NEED THIS?
			//				if (m_form.IsPulseEnabled() && (set.color.imageInfo.width < 1280 || set.color.imageInfo.height < 720))
			//				{
			//					captureMgr.FilterByStreamProfiles(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 0);
			//				}
			m_senseInput.SenseManager.captureManager.FilterByStreamProfiles(streamProfile);
		}

		// Ensure that the faceModule exists
		PXCMFaceModule faceModule = m_senseInput.SenseManager.QueryFace();
		if (faceModule == null ){
			throw new Exception("QueryFace returned null");
		}

		PXCMFaceConfiguration moduleConfiguration = faceModule.CreateActiveConfiguration();
		if (moduleConfiguration == null)
		{
			throw new Exception("FaceConfiguration null");
		}

		moduleConfiguration.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH);
		moduleConfiguration.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_RIGHT_TO_LEFT;
		moduleConfiguration.detection.maxTrackedFaces = numDetections;
		moduleConfiguration.landmarks.maxTrackedFaces = numLandmarks;
		moduleConfiguration.pose.maxTrackedFaces = numPoses;
		///////////////////

		PXCMFaceConfiguration.ExpressionsConfiguration econfiguration = moduleConfiguration.QueryExpressions();
		if (econfiguration == null)
		{
			throw new Exception("ExpressionsConfiguration null");
		}
		econfiguration.properties.maxTrackedFaces = numExpressions;
		
		econfiguration.EnableAllExpressions();
		moduleConfiguration.detection.isEnabled = true;
		moduleConfiguration.landmarks.isEnabled = true;
		moduleConfiguration.pose.isEnabled = true;
		econfiguration.Enable();
		
		PXCMFaceConfiguration.PulseConfiguration pulseConfiguration = moduleConfiguration.QueryPulse();
		if (pulseConfiguration == null)
		{
			throw new Exception("PulseConfiguration null");
		}
		
		pulseConfiguration.properties.maxTrackedFaces = m_form.NumPulse;
		pulseConfiguration.Enable();




		


		////////////////


		PXCMFaceConfiguration.RecognitionConfiguration qrecognition = moduleConfiguration.QueryRecognition();
		if (qrecognition == null)
		{
			throw new Exception("PXCMFaceConfiguration.RecognitionConfiguration null");
		}
		qrecognition.Enable();

		// Skipped conversion of seemingly not needed code?

		pxcmStatus applyChangesStatus = moduleConfiguration.ApplyChanges();

	}
	
	// Update is called once per frame
	void Update () {
		lock (this) {
			if (m_sample == null)
				return;


			// discard the current sample
			m_sample.Dispose ();
			m_sample = null;
		}
	}


	void OnSample (PXCMCapture.Sample sample)
	{
		if (shuttingDown) return;
		
		lock(this) {
			if (m_sample!=null) m_sample.Dispose();
			m_sample=sample.color;
			m_sample.QueryInstance<PXCMAddRef>().AddRef();
		}
	}

	void OnShutdown ()
	{
		shuttingDown = true;

		lock(this) {
			if (m_sample!=null){
				m_sample.Dispose();
				m_sample=null;
			}
		}
	}
}
