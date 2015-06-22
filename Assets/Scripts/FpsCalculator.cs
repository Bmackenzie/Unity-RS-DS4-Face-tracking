/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2014 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using System;
using System.Diagnostics;

/// <summary>
/// Class that is used to compute the FPS of the system.
/// </summary>
public class FpsCalculator
{
    /// <summary>
    /// Most recently obtained frame per second number.
    /// </summary>
    private float m_curFPS; 

    /// <summary>
    /// Gets the current FPS.
    /// </summary>
    public float CurrentFPS
    { get { return m_curFPS; } }

    /// <summary>
    /// Previous recorded time stamp.
    /// </summary>
    private long m_prevRecordedTimestamp;

    /// <summary>
    /// Number frames for which the period is updated.
    /// </summary>
    private int m_frameCount;

    /// <summary>
    /// Frequency of the stopwach.
    /// </summary>
    private long m_timeFreq;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public FpsCalculator()
    {
        m_timeFreq = Stopwatch.Frequency;
        m_curFPS = 0;
        m_prevRecordedTimestamp = Stopwatch.GetTimestamp();
        m_frameCount = 0;
    }

    /// <summary>
    /// Updates the systems latest FPS reading.
    /// </summary>
    public void UpdateFramePerSecond()
    {
        long period = Stopwatch.GetTimestamp() - m_prevRecordedTimestamp;

        ++m_frameCount;

        if (period > m_timeFreq)
        {
            m_curFPS = ((float)m_frameCount / period) * m_timeFreq;
            m_frameCount = 0;
            m_prevRecordedTimestamp = Stopwatch.GetTimestamp();                
        }                            
    }
}