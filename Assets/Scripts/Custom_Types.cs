/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2015 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using System.Collections.Generic;
/*
/// <summary>
/// Vector3 representation independent from the one in UnityEngine.Vector3.
/// </summary>
public class Vector3f
{
    public float x, y, z;
    public Vector3f()
    {
        x = y = z = 0.0f;
    }

    /// <summary>
    /// Converts the Vector3 representation to a float array.
    /// </summary>
    /// <returns>A float array containing the vector3 data in the format float[]{x,y,z}.</returns>
    public float[] ToFloatArray()
    {
        return new float[] { x, y, z };
    }
};
*/
public class CameraIntrinsics {
	public PXCMSizeI32  size;
	public PXCMPointF32 principalPoint;
	public PXCMPointF32 focalLength;
};