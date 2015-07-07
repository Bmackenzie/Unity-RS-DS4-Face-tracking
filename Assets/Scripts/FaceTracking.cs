using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ProtoTurtle.BitmapDrawing;

/// <summary>
/// Excellent texture drawing extension provided by proto turtle @https://github.com/ProtoTurtle/UnityBitmapDrawing
/// </summary>

public class FaceTracking : MonoBehaviour {

    public PXCMSenseManager SenseManager;
    public PXCMSession Session;
    public PXCMCaptureManager captureMgr;
    public PXCMFaceData moduleOutput;
	public ArrayList faceLocations= new ArrayList();

	/// Transform of the texture to display the rgb image.
	private Transform m_rgbDisplay;
	/// Texture to display RGB image.
	private Texture2D m_texColor;

	// Use this for initialization
    void Start()
    {
		m_texColor = new Texture2D(640,480,TextureFormat.RGBA32,false);
		
		// Tie RGB texture to display object
		m_rgbDisplay = this.transform;
		m_rgbDisplay.GetComponent<Renderer>().material.SetTexture("mainTex", m_texColor);

        // Create an instance of the sense manager interface
        Session = PXCMSession.CreateInstance();
        if (Session != null)
        {
            // Create SDK interface instance
            SenseManager = Session.CreateSenseManager();
            if (SenseManager != null)
            {
                // Retrieve captur manager instance
                PXCMCaptureManager captureMgr = SenseManager.captureManager;
                if (captureMgr != null)
                {
                    // Enable face module
                    SenseManager.EnableFace();

                    // Retrieve face module instance
                    PXCMFaceModule faceModule = SenseManager.QueryFace();
                    if (faceModule != null)
                    {
                        // Create and apply configurations
                        PXCMFaceConfiguration moduleConfiguration = faceModule.CreateActiveConfiguration();
                        if (moduleConfiguration != null)
                        {
                            moduleConfiguration.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH);
                            moduleConfiguration.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_RIGHT_TO_LEFT;
                            moduleConfiguration.detection.maxTrackedFaces = 5;
                            moduleConfiguration.detection.isEnabled = true;
							moduleConfiguration.pose.isEnabled = false;
							moduleConfiguration.EnableAllAlerts();
                            moduleConfiguration.SubscribeAlert(FaceAlertHandler);

                            pxcmStatus applyChangesStatus = moduleConfiguration.ApplyChanges();
                            if (applyChangesStatus == pxcmStatus.PXCM_STATUS_NO_ERROR)
                            {
                                // Initialize the SenseManager pipeline now that we have completed configuration. 
                                pxcmStatus status = SenseManager.Init();
                                if (status == pxcmStatus.PXCM_STATUS_NO_ERROR)
                                {
                                    // retrieve the facedata interface instance (interface to manage face module output data)
                                    moduleOutput = faceModule.CreateOutput();
                                    if (moduleOutput != null)
                                    {
										// Enumarate devices
										EnumDevices();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

	void OnDisable()
	{
		SenseManager.Dispose ();
	}

	void EnumDevices()
	{
		// session is a PXCMSession instance
		PXCMSession.ImplDesc desc1=new PXCMSession.ImplDesc();
		desc1.group=PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
		desc1.subgroup=PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
		
		for (int m=0;; m++) {
			PXCMSession.ImplDesc desc2;
			if (Session.QueryImpl (desc1, m, out desc2) < pxcmStatus.PXCM_STATUS_NO_ERROR)
				break;
			Debug.Log ("Module " + m + ": " + desc2.friendlyName);

			if (desc2.friendlyName.Contains ("DS4")) {
				PXCMCapture capture;
				Session.CreateImpl<PXCMCapture> (desc2, out capture);
			
				// print out all device information
				for (int d=0;; d++) {
					PXCMCapture.DeviceInfo dinfo;
					if (capture.QueryDeviceInfo (d, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR)
						break;
					Debug.Log ("Device " + d + ": " + dinfo.streams);
				}
				capture.Dispose ();
			}
		}
	}

    private bool CheckForDepthStream(PXCMCapture.Device.StreamProfileSet profiles, PXCMFaceModule faceModule)
    {
        bool status = false;
        PXCMFaceConfiguration faceConfiguration = faceModule.CreateActiveConfiguration();
        if (faceConfiguration != null)
        {

            PXCMFaceConfiguration.TrackingModeType trackingMode = faceConfiguration.GetTrackingMode();
            faceConfiguration.Dispose();

            if (trackingMode == PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH) status = true;
            if (profiles.depth.imageInfo.format == PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH) status = true;
        }

        return status;
    }


    private void FaceAlertHandler(PXCMFaceData.AlertData alert)
    {
        Debug.Log(alert.label.ToString());
    }

	// Update is called once per frame
    void Update()
    {
		// Clear the face data from prior updates
		faceLocations.Clear ();

        pxcmStatus status = SenseManager.AcquireFrame(true);
        if (status == pxcmStatus.PXCM_STATUS_NO_ERROR)
        {
			/*
			// Query to get the image for display
			var m_sample = SenseManager.QuerySample();
			if (m_sample != null)
			{
				// Get ad display the color imag
				PXCMImage.ImageData data = null;
				pxcmStatus sts = m_sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data);
				if (sts >= pxcmStatus.PXCM_STATUS_NO_ERROR)
				{
					if(data != null){
						if(data.planes.Length > 0){
							Debug.Log("data exported");
							data.ToTexture2D(1, texture);
							meshy.material.mainTexture = texture;
						}
					}
					m_sample.color.ReleaseAccess(data);
				}
				
				m_sample.color.ReleaseAccess(data);
				m_sample.color.Dispose();
			}
			*/
			// Query the sample for faces. 
            var sample = SenseManager.QueryFaceSample();
			var image_sample = SenseManager.QuerySample();
            if (sample != null)
            {
                moduleOutput.Update();
				PXCMFaceData.Face[] faces = moduleOutput.QueryFaces();
				if(faces != null)
				{
					Debug.Log("Face count: " + faces.Length);	
				}

				foreach (PXCMFaceData.Face face in faces)
				{
					PXCMFaceData.DetectionData detData = face.QueryDetection();
					
					PXCMRectI32 rect;
					detData.QueryBoundingRect(out rect);
					
					float depth;
					detData.QueryFaceAverageDepth(out depth);
					
					FaceLocation faceLoc = new FaceLocation(rect.w, rect.h, rect.x, rect.y, depth);
					faceLocations.Add(faceLoc);
					Debug.Log(faceLoc);
				}
				DisplayImage(image_sample.color);

            }
        }
        SenseManager.ReleaseFrame();
    }

	/// <summary>
	/// Displays the image.
	/// </summary>
	/// <param name="image">Image.</param>
	public void DisplayImage(PXCMImage image)
	{
		//image.info.width
		if (image != null) {
			//PXCMImage.ImageData data = new PXCMImage.ImageData();
			PXCMImage.ImageData data = null;
			pxcmStatus sts = image.AcquireAccess (PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data);
			if (sts == pxcmStatus.PXCM_STATUS_NO_ERROR) {
				data.ToTexture2D (0, m_texColor);
				image.ReleaseAccess (data);
				// draw face locations
				foreach(FaceLocation face in faceLocations){
					Rect face_rect = new Rect(face.getX(), face.getY(), face.getWidth(), face.getHeight());
					m_texColor.DrawRectangle(face_rect,Color.red);
				}
				m_texColor.Apply ();

				Debug.Log ("displaying image");
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

		public override string ToString(){
			string returnString = "Single FaceLocation Data:\n";
			returnString += "\tWidth : " + w + "\n";
			returnString += "\tHeight : " + h + "\n";
			returnString += "\tX-Coord : " + x + "\n";
			returnString += "\tY-Coord : " + y + "\n";
			returnString += "\tDepth : " + z + "\n";

			return returnString;
		}
	}
}
