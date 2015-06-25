/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2015 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using System.Threading;
using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;

public class OpenCvViewer : MonoBehaviour
{
	/// <summary>
	/// The sense input interface instance.
	/// </summary>
	public SenseInput m_senseInput;

    /// <summary>
    /// Maxtrix used to store the canny edge
    /// </summary>
    static IplImage matrix;

    /// <summary>
    /// Transform of the texture to display the rgb image.
    /// </summary>
    private Transform m_rgbDisplay;

    /// <summary>
    /// Texture to display RGB image.
    /// </summary>
    private Texture2D m_texColorModified;
    private Texture2D m_texColorOriginal;

    /// <summary>
    /// saved sample for display
    /// </summary>
    private PXCMImage m_sample;

    private bool shuttingDown = false;

    /// <summary>
    /// Event handler to notify display texture has been updated
    void Start()
    {
        m_senseInput.m_OnSample += OnSample;
        m_senseInput.m_ShutDown += OnShutdown;

        m_texColorModified = new Texture2D(m_senseInput.m_width, m_senseInput.m_height, TextureFormat.RGBA32, false);
        m_texColorOriginal = new Texture2D(m_senseInput.m_width, m_senseInput.m_height, TextureFormat.RGBA32, false);
        matrix = new IplImage(m_senseInput.m_width, m_senseInput.m_height, BitDepth.U8, 3);

        // Tie RGB texture to display object
        m_rgbDisplay = this.transform;
        m_rgbDisplay.GetComponent<Renderer>().material.SetTexture("mainTex", m_texColorModified);
    }

    void Update()
    {
        lock (this)
        {
            if (m_sample == null) return;

            // display the color image
            PXCMImage.ImageData data;
            pxcmStatus sts = m_sample.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data);
            if (sts >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                data.ToTexture2D(0, m_texColorOriginal);

                //Store Originals pixels
                Color32[] pix = m_texColorOriginal.GetPixels32();
                System.Array.Reverse(pix);
                //Texture2D destTex = new Texture2D(m_texColorOriginal.width, m_texColorOriginal.height);
                m_texColorModified.SetPixels32(pix);
                m_texColorModified.Apply();

                m_sample.ReleaseAccess(data);
                m_texColorModified.Apply();
            }

            // discard the current sample
            m_sample.Dispose();
            m_sample = null;
        }

        Texture2DtoIplImage();
        IplImage cny = new IplImage(m_senseInput.m_width, m_senseInput.m_height, BitDepth.U8, 1);
        matrix.CvtColor(cny, ColorConversion.RgbToGray);
        Cv.Canny(cny, cny, 50, 75, ApertureSize.Size3);
        Cv.CvtColor(cny, matrix, ColorConversion.GrayToBgr);
        IplImageToTexture2D();
    }

    /// <summary>
    /// Callback function:
    /// Save color sample by adding a reference for display later. 
    /// </summary>
    /// <param name="sample">Sample.</param>
    private void OnSample(PXCMCapture.Sample sample)
    {
        if (shuttingDown) return;

        lock (this)
        {
            if (m_sample != null) m_sample.Dispose();
            m_sample = sample.color;
            m_sample.QueryInstance<PXCMAddRef>().AddRef();
        }
    }

    private void OnShutdown()
    {
        shuttingDown = true;

        lock (this)
        {
            if (m_sample != null)
            {
                m_sample.Dispose();
                m_sample = null;
            }
        }
    }

    void IplImageToTexture2D()
    {
        int jBackwards = m_senseInput.m_height;

        for (int i = 0; i < m_senseInput.m_height; i++)
        {
            for (int j = 0; j < m_senseInput.m_width; j++)
            {
                float b = (float)matrix[i, j].Val0;
                float g = (float)matrix[i, j].Val1;
                float r = (float)matrix[i, j].Val2;
                Color color = new Color(r / 255.0f, g / 255.0f, b / 255.0f);

                if (color != new Color(0f, 0f, 0f)){
                    jBackwards = m_senseInput.m_height - i - 1; // notice it is jBackward and i
                    m_texColorModified.SetPixel(j, jBackwards, Color.black);
                }
                else
                {
                    jBackwards = m_senseInput.m_height - i - 1; // notice it is jBackward and i
                    m_texColorModified.SetPixel(j, jBackwards, m_texColorOriginal.GetPixel(-j, i)); // need to convert the original to IPLImage for this to work. :(
                }
            }
        }
        m_texColorModified.Apply();
    }

    void Texture2DtoIplImage()
    {
        int jBackwards = m_senseInput.m_height;

        for (int v = 0; v < m_senseInput.m_height; ++v)
        {
            for (int u = 0; u < m_senseInput.m_width; ++u)
            {
                CvScalar col = new CvScalar();
                col.Val0 = (double)m_texColorModified.GetPixel(u, v).b * 255;
                col.Val1 = (double)m_texColorModified.GetPixel(u, v).g * 255;
                col.Val2 = (double)m_texColorModified.GetPixel(u, v).r * 255;

                jBackwards = m_senseInput.m_height - v - 1;

                matrix.Set2D(jBackwards, u, col);
                //matrix [jBackwards, u] = col;
            }
        }
        //Cv.SaveImage ("C:\\Hasan.jpg", matrix);
    }
}