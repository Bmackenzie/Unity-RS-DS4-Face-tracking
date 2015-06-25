using UnityEngine;
using System.Collections;
using System;
//TODO
/*
 * Move initialization to SenseInput form Start
 * 
 * x OnModuleProcessedFrame
 * [Initialize Differently?]
 * 
 * x Set global list,
 * x Make clustom class, store rectangular data, as well as average depth
 * --> Should I clear it in update?!
 * 
 * Add to camera so it is called
 * 
 * Somewhere else, access the public list of positions.
 */
public class FaceTracker : MonoBehaviour {

	public SenseInput m_senseInput;
	private bool shuttingDown = false;

	private PXCMCapture.Sample m_sample;
	
	private int numDetections = 5;

	public  ArrayList faceLocations;

	// Use this for initialization
	// Go to SenseInputStart
	void Start () {
		m_senseInput.m_OnSample+=OnSample;
		m_senseInput.m_ShutDown+=OnShutdown;
		m_senseInput.m_OnData += OnModuleProcessedFrame;

		if (m_senseInput.SenseManager == null){
			throw new Exception("PXCMSenseManager null");
		}
		m_senseInput.SenseManager.EnableFace ();
		PXCMFaceModule faceModule = m_senseInput.SenseManager.QueryFace ();

		if (faceModule == null ){
			throw new Exception("QueryFace returned null");
		}

		PXCMFaceConfiguration moduleConfiguration = faceModule.CreateActiveConfiguration();
		if (moduleConfiguration == null)
		{
			throw new Exception("FaceConfiguration null");
		}

		moduleConfiguration.detection.isEnabled = true;

		// Skipped conversion of seemingly not needed code?

		pxcmStatus applyChangesStatus = moduleConfiguration.ApplyChanges();
		// Should I dispose of the faceModule and configuration?

		m_senseInput.SenseManager.Init ();

	}
	
	// Update is called once per frame
	void Update () {
		lock (this) {
			//if (m_sample == null)
			//	return;
			//PXCMFaceModule fm=m_senseInput.SenseManager.QueryFace();
			//PXCMFaceData fdata= fm.CreateOutput();
			//Debug.Log(fdata.QueryNumberOfDetectedFaces());

			//fdata.Dispose();
			//fm.Dispose();

			// discard the current sample
			//m_sample.Dispose ();

			////////////////////////////////////
			/// Should I clear the arraylist here?!
			m_sample = null;
		}
	}

	void OnModuleProcessedFrame(int mid, PXCMBase module, PXCMCapture.Sample sample){

		if (shuttingDown) {
			return;
		}

		if (mid == PXCMFaceModule.CUID) {
			PXCMFaceModule fm = m_senseInput.SenseManager.QueryFace();
			PXCMFaceData.Face[] faces = fm.CreateOutput().QueryFaces();
			foreach (PXCMFaceData.Face face in faces){
				PXCMFaceData.DetectionData detData = face.QueryDetection();

				PXCMRectI32 rect;
				detData.QueryBoundingRect(out rect);

				float depth;
				detData.QueryFaceAverageDepth(out depth);

				FaceLocation faceLoc = new FaceLocation(rect.w, rect.h, rect.x, rect.y, depth);
				faceLocations.Add(faceLoc);
			}
			return;
		}
		return;
	}

	// I don't think I reall y need this...
	void OnSample (PXCMCapture.Sample sample)
	{
		if (shuttingDown) return;

		//lock(this) {
		//	if (m_sample!=null) m_sample.Dispose();
		//	m_sample=sample;
		//	m_sample.QueryInstance<PXCMAddRef>().AddRef();
		//}
	}

	void OnShutdown ()
	{
		shuttingDown = true;

		lock(this) {
			if (m_sample!=null){
		//		m_sample.Dispose();
				m_sample=null;
			}
		}
	}

	public class FaceLocation{
		private readonly int w;
		private readonly int h;
		private readonly int x;
		private readonly int y;
		private readonly float z;

		public FaceLocation(int w, int h, int x, int y, float z){
			this.w=w;
			this.h=h;
			this.x=x;
			this.y=y;
			this.z=z;
		}

		public int getWidth(){
			return this.w;
		}

		public int getHeight(){
			return this.h;
		}

		public int getX(){
			return this.x;
		}

		public int getY(){
			return this.y;
		}

		public float getDepth(){
			return this.z;
		}
	}
}
