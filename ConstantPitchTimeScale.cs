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
    public static float ChangeFactor { get; set; }
    public static int ScaledLength { get; private set; }

    float[,] _bins;
    float[] _inputSamples;
    float[] _blendedSamples;
    int _binLength;
    int _halfBinLength;
    int _numberOfBins;
    int _rampLength;

    public void Create(float[] inputSamples, float changeFactor = 1f, int binLength = 4096, float rampProportion = 0.25f)
    {
        if (binLength > inputSamples.Length)
            throw new ArgumentOutOfRangeException($"binLength: {binLength} must be less than inputSample.Length: {inputSamples.Length}");

        if (rampProportion > 1f || rampProportion < 0f)
            throw new ArgumentOutOfRangeException("rampProportion must be between 0 and 1");

        if (inputSamples == null)
            throw new ArgumentException("inputSamples cannot be null");

        _inputSamples = new float[inputSamples.Length];
        _rampLength = (int)((float)binLength * rampProportion);
        _halfBinLength = binLength / 2;
        _binLength = binLength;
        _numberOfBins = (int)Math.Ceiling((float)_inputSamples.Length / (float)_halfBinLength);
        inputSamples.CopyTo(_inputSamples, 0);

        if (changeFactor != 1f)
            SetChangeFactor(changeFactor);
    }

    public void SetChangeFactor(float changeFactor)
    {
        if (changeFactor >= 1.5f)
            throw new ArgumentOutOfRangeException("changeFactor must be less than 1.5");

        ChangeFactor = changeFactor;
        ScaledLength = (int)Math.Ceiling((float)_inputSamples.Length * changeFactor);
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
        for (var positionInInputSamples = 0; positionInInputSamples < _inputSamples.Length; ++positionInInputSamples)
        {
            _bins[LeftHalfBinNumber(positionInInputSamples) - 1, positionInInputSamples % _halfBinLength] = _inputSamples[positionInInputSamples];
            if (RightHalfBinNumber(positionInInputSamples) > 0)
                _bins[RightHalfBinNumber(positionInInputSamples) - 1, (positionInInputSamples % _halfBinLength) + _halfBinLength] = _inputSamples[positionInInputSamples];
        }
    }

    int LeftHalfBinNumber(int positionInInputSamples) => (int)((positionInInputSamples / _halfBinLength) + 1);
    int RightHalfBinNumber(int positionInInputSamples) => LeftHalfBinNumber(positionInInputSamples) - 1;

    void BlendBins()
    {
        int halfOfWindowLength = (int)Math.Ceiling((float)_halfBinLength * ChangeFactor);
        _blendedSamples = new float[ScaledLength];
        var positionInWindow = 0;
        for (var positionInBlendedSamples = 0; positionInBlendedSamples < _blendedSamples.Length; ++positionInBlendedSamples)
        {
            bool isWithinRamp = ((int)((positionInBlendedSamples - _rampLength) / halfOfWindowLength) != (int)(positionInBlendedSamples / halfOfWindowLength)) && positionInBlendedSamples > _rampLength;
            bool isWithinLastBin = (positionInBlendedSamples / halfOfWindowLength) + 1 == _numberOfBins;
            if (isWithinRamp && !isWithinLastBin)
            {
                float rightScaleAmount = (float)positionInWindow / (float)_rampLength;
                float leftScaleAmount = (float)(1 - rightScaleAmount);
                float rightSampleValue = _bins[(positionInBlendedSamples / halfOfWindowLength), positionInBlendedSamples % halfOfWindowLength];
                float leftSampleValue = _bins[(positionInBlendedSamples / halfOfWindowLength) - 1, (positionInBlendedSamples % halfOfWindowLength) + halfOfWindowLength];
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
