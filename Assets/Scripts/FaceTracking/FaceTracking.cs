using UnityEngine;
using System.Collections;

public class FaceTracking : MonoBehaviour {

    public PXCMSenseManager SenseManager;
    public PXCMSession Session;
    public PXCMCaptureManager captureMgr;
    public PXCMFaceData moduleOutput;

	// Use this for initialization
    void Start()
    {
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
                            moduleConfiguration.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR);
                            moduleConfiguration.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_RIGHT_TO_LEFT;
                            moduleConfiguration.detection.maxTrackedFaces = 5;
                            moduleConfiguration.detection.isEnabled = true;
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
                                        // Create instance for video stream parameters
                                        PXCMCapture.Device.StreamProfileSet profiles;
                                        //Retrieve the capture device intance 
                                        PXCMCapture.Device device = captureMgr.QueryDevice();

                                        if (device != null)
                                        {
                                            device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 0, out profiles);
                                            if (CheckForDepthStream(profiles, faceModule) == false)
                                            {
                                                Debug.Log("Depth stream is not supported for device");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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
        pxcmStatus status = SenseManager.AcquireFrame(true);
        Debug.Log("status: " + status);
        if (status == pxcmStatus.PXCM_STATUS_NO_ERROR)
        {
            var sample = SenseManager.QueryFaceSample();
            if (sample != null)
            {
                moduleOutput.Update();
            }
        }
        SenseManager.ReleaseFrame();
    }

    void SimplePipeline() { 
    }
}
