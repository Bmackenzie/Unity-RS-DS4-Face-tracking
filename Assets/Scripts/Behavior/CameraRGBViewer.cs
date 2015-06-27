/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2015 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using UnityEngine;
using System.Threading;

public class CameraRGBViewer : MonoBehaviour
{
	public SenseInput m_senseInput;

    /// <summary>
    /// Transform of the texture to display the rgb image.
    /// </summary>
    private Transform m_rgbDisplay;

    /// <summary>
    /// Texture to display RGB image.
    /// </summary>
    private Texture2D m_texColor;

	/// <summary>
	/// saved sample for display
	/// </summary>
	private PXCMImage m_sample;

	private bool shuttingDown=false;

    /// <summary>
    /// Event handler to notify display texture has been updated
    void Start()
    {
		Debug.Log (m_senseInput);
		m_senseInput.m_OnSample+=OnSample;
		m_senseInput.m_ShutDown+=OnShutdown;

		m_texColor = new Texture2D(m_senseInput.m_width,m_senseInput.m_height,TextureFormat.RGBA32,false);

        // Tie RGB texture to display object
        m_rgbDisplay = this.transform;
        m_rgbDisplay.GetComponent<Renderer>().material.SetTexture("mainTex", m_texColor);
    }

    void Update()
    {
		lock(this) {
			if (m_sample==null) return;

			// display the color image
			PXCMImage.ImageData data;
			pxcmStatus sts=m_sample.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data);
			if (sts>=pxcmStatus.PXCM_STATUS_NO_ERROR) {
				data.ToTexture2D(0, m_texColor);
				m_sample.ReleaseAccess(data);
				m_texColor.Apply();
			}

			// discard the current sample
			m_sample.Dispose();
			m_sample=null;
		}
    }

	/// <summary>
	/// Callback function:
	/// Save color sample by adding a reference for display later. 
	/// </summary>
	/// <param name="sample">Sample.</param>
    private void OnSample(PXCMCapture.Sample sample) {
		if (shuttingDown) return;

		lock(this) {
			if (m_sample!=null) m_sample.Dispose();
			m_sample=sample.color;
			m_sample.QueryInstance<PXCMAddRef>().AddRef();
		}
    }

	private void OnShutdown() {
		shuttingDown=true;

		lock(this) {
			if (m_sample!=null) {
				m_sample.Dispose();
				m_sample=null;
			}
		}
	}
}