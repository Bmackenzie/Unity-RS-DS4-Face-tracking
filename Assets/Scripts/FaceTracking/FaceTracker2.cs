/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FaceTracker2 : MonoBehaviour 
{
	/// The text components that will display the onscreen positions
	public Text msg1;
	public Text msg2;
	public Text msg3;
	public Text msg4;
	public Text msg5;
	public Text msg6;

	/// The Face Module interface instance
	public PXCMFaceModule face = null;
	
	/// The face data interface instance
	public PXCMFaceData face_data = null;

	/// The face data structure
	PXCMFaceData.Face[] faceData;

	/// The Face configuration instance
	private PXCMFaceConfiguration fcfg;

    PXCMSenseManager sm = null;
	
	// Use this for initialization
	void Start () 
	{
		Debug.Log("Survived to Start beginning");
		/* Initialize a PXCMSenseManager instance */
		sm = PXCMSenseManager.CreateInstance();
		if (sm != null)
		{
			/* Enable face tracking and configure the face module */
			pxcmStatus sts = sm.EnableFace();
			if(sts == pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
				/* Face module interface instance */
				face = sm.QueryFace(); 
				/* Face data interface instance */
				face_data = face.CreateOutput();

				// Create face configuration instance and configure 
				fcfg = face.CreateActiveConfiguration ();
				fcfg.detection.isEnabled=true;
				fcfg.EnableAllAlerts ();
				fcfg.SubscribeAlert(OnFiredAlert);
				fcfg.ApplyChanges ();
				fcfg.Dispose ();

				/* Initialize the execution pipeline */ 
				if (sm.Init() != pxcmStatus.PXCM_STATUS_NO_ERROR) 
				{
					OnDisable();
				}
			}
		}
		Debug.Log("Survived to Start end");
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (sm != null)
		{
			Debug.Log("sm not null");
			/* Wait until any frame data is available */
			pxcmStatus status = sm.AcquireFrame(false, 0);
			if ( status == pxcmStatus.PXCM_STATUS_NO_ERROR) 
			{
				Debug.Log("aquired frame");
				/* Retrieve latest face data */
                status = face_data.Update();
                if (status == pxcmStatus.PXCM_STATUS_NO_ERROR)
				{
					Debug.Log("updating");
					Debug.Log ("Faces: " + face_data.QueryNumberOfDetectedFaces());
//					faceData = face_data.QueryFaces();
//
//					PXCMRectI32 rect;
//					detData.QueryBoundingRect(out rect);
//
//					float depth;
//					detData.QueryFaceAverageDepth(out depth);
//
//					FaceLocation faceLoc = new FaceLocation(rect.w, rect.h, rect.x, rect.y, depth);
//					faceLocations.Add(faceLoc);
					
				}
				/* Now, release the current frame so we can process the next frame */
				sm.ReleaseFrame();
			}else{
				Debug.Log("Failed: "+ status);
			}
		} 
	}

	/* Capture current frames extremity */
	private void TrackExtremity(PXCMFaceData faceOutput)
	{
		/* //Get hand by time of appearence
		if (handOutput.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_LEFTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				Debug.Log("LeftMost Extremity Position = " + extremityPoint.pointWorld);

			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_LEFTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg1.text = "Left Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_RIGHTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg2.text = "Right Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_TOPMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg3.text = "Top Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_BOTTOMMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg4.text = "Bottom Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_CENTER, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg5.text = "Center Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_CLOSEST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg6.text = "Closest Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";

		} */
	}

	void OnFiredAlert(PXCMFaceData.AlertData data)
	{
		Debug.Log(data.label.ToString ());
	}

	void OnDisable() 
	{
		Debug.Log("Survived to OnDisable");
		/* Dispose face data instance*/ 
		if(face_data != null)
		{
			face_data.Dispose();
			face_data = null;
		}
		
		/* Dispose face module instance*/ 
		if(face != null)
		{
			face.Dispose ();
			face = null;
		}
		
		/* Dispose sense manager instance*/ 
		if (sm != null)
		{
			sm.Dispose();
			sm = null;
		}
	}
}



