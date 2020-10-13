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

//This class creates time stretched audio without altering the pitch. 
public class ConstantPitchTimeScale : MonoBehaviour
{
    #region Pitch Change Parameters
    public static float PercentChange { get; set; }
    public static int BlendedLength { get; private set; }
    #endregion

    float[,] _bins;
    float[] _inputSamples;
    float[] _blendedSamples;
    int _binLength;
    int _halfBinLength;
    int _numberOfBins;
    int _rampLength;

    public void Create(float[] inputSamples, int binSize = 4096, float rampProportion = 0.25f)
    {
        _inputSamples = new float[inputSamples.Length];
        _rampLength = (int)((float)binSize * rampProportion);
        _halfBinLength = binSize / 2;
        _binLength = binSize;
        _numberOfBins = (int)Math.Ceiling((float)_inputSamples.Length / (float)_halfBinLength);
        inputSamples.CopyTo(_inputSamples, 0);
    }

    public void SetPercentChange(float percentChange)
    {
        PercentChange = percentChange;
        BlendedLength = (int)Math.Ceiling((float)_inputSamples.Length * PercentChange);
    }

    public void GetModifiedAudio(out float[] targetSamples)
    {
        InitializeBins();
        BlendBins();
        targetSamples = new float[_blendedSamples.Length];
        _blendedSamples.CopyTo(targetSamples, 0);
    }

    void InitializeBins()
    {
        _bins = new float[_numberOfBins, _binLength];
        for (var i = 0; i < _inputSamples.Length; ++i)
        {
            _bins[LeftHalfBinNumber(i) - 1, i % _halfBinLength] = _inputSamples[i];
            if (RightHalfBinNumber(i) > 0)
                _bins[RightHalfBinNumber(i) - 1, (i % _halfBinLength) + _halfBinLength] = _inputSamples[i];
        }
    }

    int LeftHalfBinNumber(int i) => (int)((i / _halfBinLength) + 1);
    int RightHalfBinNumber(int i) => LeftHalfBinNumber(i) - 1;

    void BlendBins()
    {
        int halfOfWindowLength = (int)Math.Ceiling((float)_binLength * PercentChange / 2f);
        _blendedSamples = new float[BlendedLength];
        var positionInWindow = 0;
        for (var positionInBlendedSamples = 0; positionInBlendedSamples < BlendedLength; ++positionInBlendedSamples)
        {
            bool isWithinRamp = ((int)((positionInBlendedSamples - _rampLength) / halfOfWindowLength) != (int)(positionInBlendedSamples / halfOfWindowLength)) && positionInBlendedSamples > _rampLength;
            bool isLastWindow = (positionInBlendedSamples / halfOfWindowLength) + 1 == _numberOfBins;
            if (isWithinRamp && !isLastWindow)
            {
                float rightScaleAmount = (float)positionInWindow / (float)_rampLength;
                float leftScaleAmount = (float)(1 - rightScaleAmount);
                float rightSampleValue = _bins[(positionInBlendedSamples / halfOfWindowLength), positionInBlendedSamples % halfOfWindowLength];
                float leftSampleValue = _bins[positionInBlendedSamples / halfOfWindowLength - 1, (positionInBlendedSamples % halfOfWindowLength) + halfOfWindowLength];
                _blendedSamples[positionInBlendedSamples] = leftSampleValue * leftScaleAmount + rightSampleValue * rightScaleAmount;
                ++positionInWindow;
            }
            else
            {
                _blendedSamples[positionInBlendedSamples] = _bins[positionInBlendedSamples / halfOfWindowLength, positionInBlendedSamples % halfOfWindowLength];
                positionInWindow = 0;
            }
        }
    }
}
