/* This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using UnityEngine;

public class PitchModifier : MonoBehaviour
{
    #region Pitch Change Parameters
    public int BinSize { get; set; }
    public float [] InputSamples { get; set; }
    public float PercentChange { get; set; }
    public int ProcessedLength { get; private set; }
    #endregion

    private float[,] bins;
    private int binSampleOffset = 2048;
    private float[] processedSamples;
    private int rampSampleCount = 1024;
    private int numberOfBins;

    virtual public PitchModifier(float[] inputSamples, int binSize)
    {
        InputSamples = new float[inputSamples.Length];
        Array.Copy(inputSamples, InputSamples, inputSamples.Length);

        BinSize = binSize;
    }

    virtual public void FillBins()
    {
        numberOfBins = (int) Math.Ceiling((float)InputSamples.Length / (float)binSampleOffset);
        bins = new float[numberOfBins,BinSize];
        
        for (var i = 0; i < InputSamples.Length; ++i)
        {
            bins[GetP1(i) - 1, i % binSampleOffset] = InputSamples[i];
            
            if (GetP2(i) > 0)
            {
                bins[GetP2(i) - 1, (i % binSampleOffset) + binSampleOffset] = InputSamples[i];
            }
        }
    }

    protected int GetP1(int i)
    {
        return (int)((i / binSampleOffset) + 1);
    }

    protected int GetP2(int i)
    {
        return (GetP1(i) - 1);
    }

    protected void ProcessBins()
    {
        var mySwitchPoint = GetSwitchSample();
        processedSamples = new float[ProcessedLength];
        var j = 0;
        for (var i = 0; i < ProcessedLength; ++i)
        {
            var inRampWindow = ((int)((i - rampSampleCount) / mySwitchPoint) != (int)(i / mySwitchPoint))
                && (i - rampSampleCount) > 0;
            var isLastWindow = ((i / mySwitchPoint) + 1) == numberOfBins;
            if ( inRampWindow && !isLastWindow)
            {
                var newScalar = (float) ( ((float) j) / ( (float) rampSampleCount));
                var oldScalar = (float) ( ((float) (rampSampleCount - j)) / ((float) rampSampleCount));
                var oldSample = bins[i / mySwitchPoint - 1, (i % mySwitchPoint) + mySwitchPoint];
                var newSample = bins[(i / mySwitchPoint), i % mySwitchPoint];
                processedSamples[i] = oldSample * oldScalar + newSample * newScalar;
                ++j;
            }
            else
            {
                processedSamples[i] = bins[i / mySwitchPoint, i % mySwitchPoint];
                j = 0; 
            }
        }
    }

    protected int GetSwitchSample()
    {
        return (int) Math.Ceiling( ( ((float) BinSize) * PercentChange ) / 2);
    }

    protected void SetLength()
    {
        ProcessedLength = (int) Math.Ceiling( ((float) InputSamples.Length) * PercentChange);

    }

    virtual public void SetPercentChange(float percentChange)
    {
        PercentChange = percentChange;
        SetLength();
    }
    
    virtual public void GetUnModifiedAudio(float[] targetSamples)
    {
        FillBins();
        for (var i = 0; i < InputSamples.Length; ++i)
        {
            targetSamples[i] = bins[i / binSampleOffset, i % binSampleOffset];

        }
    }

    virtual public void GetModifiedAudio(float[] targetSamples)
    {
        FillBins();
        ProcessBins();

        Array.Copy(processedSamples, targetSamples, processedSamples.Length);
    }
}
