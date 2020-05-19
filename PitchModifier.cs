using System;
using UnityEngine;

public class PitchModifier : MonoBehaviour
{
    public int BinSize { get; set; }
    public float [] InputSamples { get; set; }
    public float PercentChange { get; set; }
    public int ProcessedLength { get; private set; }

    private float[,] bins;
    private int binSampleOffset = 2048;
    private float[] processedSamples;
    private int rampSampleCount = 1024;
    private int numberOfBins;

    public PitchModifier(float[] inputSamples, int binSize)
    {
        InputSamples = new float[inputSamples.Length];
        Array.Copy(inputSamples, InputSamples, inputSamples.Length);

        BinSize = binSize;
    }

    public void FillBins()
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

    int GetP1(int i)
    {
        return (int)((i / binSampleOffset) + 1);
    }

    int GetP2(int i)
    {
        return (GetP1(i) - 1);
    }

    void ProcessBins()
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

    int GetSwitchSample()
    {
        return (int) Math.Ceiling( ( ((float) BinSize) * PercentChange ) / 2);
    }

    void SetLength()
    {
        ProcessedLength = (int) Math.Ceiling( ((float) InputSamples.Length) * PercentChange);

    }

    public void SetPercentChange(float percentChange)
    {
        PercentChange = percentChange;
        SetLength();
    }
    
    public void GetUnModifiedAudio(float[] targetSamples)
    {
        FillBins();
        for (var i = 0; i < InputSamples.Length; ++i)
        {
            targetSamples[i] = bins[i / binSampleOffset, i % binSampleOffset];

        }
    }

    public void GetModifiedAudio(float[] targetSamples)
    {
        FillBins();
        ProcessBins();

        Array.Copy(processedSamples, targetSamples, processedSamples.Length);
    }
}
