/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2014 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using UnityEngine;

public class ScenePerception : MonoBehaviour 
{
    /// <summary>
    /// FPS calculator object.
    /// </summary>
    private FpsCalculator m_fpsUnityUpdate;

    /// <summary>
    /// String to display FPS.
    /// </summary>
    private string m_strFPS;

	void Awake () 
    {
        // Initialize the FPS calculator
        m_fpsUnityUpdate = new FpsCalculator();
        m_strFPS = "";
	}
	
	void Update () 
    {
        //calculate the frame rate
        m_fpsUnityUpdate.UpdateFramePerSecond();
        m_strFPS = "FPS : " + m_fpsUnityUpdate.CurrentFPS.ToString("#.0");
	}

    void OnGUI()
    {
        GUI.skin.box.hover.textColor =
        GUI.skin.box.normal.textColor =
        GUI.skin.box.active.textColor = Color.green;
        GUI.skin.box.alignment = TextAnchor.MiddleCenter;

        GUI.Box(new Rect(5, 5, 80, 30), m_strFPS);
    }
}