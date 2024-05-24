// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// Contains a collection of <see cref="CurveKey"/> points in 2D space and provides methods for evaluating features of the curve they define.
    /// </summary>
    // TODO : [TypeConverter(typeof(ExpandableObjectConverter))]
    [DataContract]
    public class Curve : ICurveEvaluator<float>
    {
        #region Private Fields

        private CurveLoopType _preLoop;
        private CurveLoopType _postLoop;
        private CurveKeyCollection _keys;

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns <c>true</c> if this curve is constant (has zero or one points); <c>false</c> otherwise.
        /// </summary>
        [DataMember]
        public bool IsConstant
        {
            get { return this._keys.Count <= 1; }
        }

        /// <summary>
        /// Defines how to handle weighting values that are less than the first control point in the curve.
        /// </summary>
        [DataMember]
        public CurveLoopType PreLoop
        {
            get { return this._preLoop; }
            set { this._preLoop = value; }
        }

        /// <summary>
        /// Defines how to handle weighting values that are greater than the last control point in the curve.
        /// </summary>
        [DataMember]
        public CurveLoopType PostLoop
        {
            get { return this._postLoop; }
            set { this._postLoop = value; }
        }

        /// <summary>
        /// The collection of curve keys.
        /// </summary>
        [DataMember]
        public CurveKeyCollection Keys
        {
            get { return this._keys; }
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Constructs a curve.
        /// </summary>
        public Curve()
        {
            this._keys = new CurveKeyCollection();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a copy of this curve.
        /// </summary>
        /// <returns>A copy of this curve.</returns>
        public Curve Clone()
        {
            Curve curve = new Curve();

            curve._keys = this._keys.Clone();
            curve._preLoop = this._preLoop;
            curve._postLoop = this._postLoop;

            return curve;
        }

        public float Evaluate(float position)
        {
            return Evaluate(position, null);
        }

        /// <summary>
        /// Evaluate the value at a position of this <see cref="Curve"/> with a specified interplolation function.
        /// </summary>
        /// <param name="position">The position on this <see cref="Curve"/>.</param>
        /// <param name="interpolant">The function to evaluate. </param>
        /// <returns>Value at the position on this <see cref="Curve"/>.</returns>
        public float Evaluate(float position, Func<float, CurveKey, CurveKey, float> interpolant = null)
        {
            if (interpolant == null)
                interpolant = DefaultInterpolant;
            if (_keys.Count == 0)
            {
            	return 0f;
            }
						
            if (_keys.Count == 1)
            {
            	return _keys[0].Value;
            }
			
            CurveKey first = _keys[0];
            CurveKey last = _keys[_keys.Count - 1];

            if (position < first.Position)
            {
                switch (this.PreLoop)
                {
                    case CurveLoopType.Constant:
                        //constant
                        return first.Value;

                    case CurveLoopType.Linear:
                        // linear y = a*x +b with a tangeant of last point
                        return first.Value - first.TangentIn * (first.Position - position);

                    case CurveLoopType.Cycle:
                        //start -> end / start -> end
                        int cycle = GetNumberOfCycle(position);
                        float virtualPos = position - (cycle * (last.Position - first.Position));
                        return Evaluate2(virtualPos, interpolant);

                    case CurveLoopType.CycleOffset:
                        //make the curve continue (with no step) so must up the curve each cycle of delta(value)
                        cycle = GetNumberOfCycle(position);
                        virtualPos = position - (cycle * (last.Position - first.Position));
                        return (Evaluate2(virtualPos, interpolant) + cycle * (last.Value - first.Value));

                    case CurveLoopType.Oscillate:
                        //go back on curve from end and target start 
                        // start-> end / end -> start
                        cycle = GetNumberOfCycle(position);
                        if (0 == cycle % 2f)//if pair
                            virtualPos = position - (cycle * (last.Position - first.Position));
                        else
                            virtualPos = last.Position - position + first.Position + (cycle * (last.Position - first.Position));
                        return Evaluate2(virtualPos, interpolant);
                }
            }
            else if (position > last.Position)
            {
                int cycle;
                switch (this.PostLoop)
                {
                    case CurveLoopType.Constant:
                        //constant
                        return last.Value;

                    case CurveLoopType.Linear:
                        // linear y = a*x +b with a tangeant of last point
                        return last.Value + first.TangentOut * (position - last.Position);

                    case CurveLoopType.Cycle:
                        //start -> end / start -> end
                        cycle = GetNumberOfCycle(position);
                        float virtualPos = position - (cycle * (last.Position - first.Position));
                        return Evaluate2(virtualPos, interpolant);

                    case CurveLoopType.CycleOffset:
                        //make the curve continue (with no step) so must up the curve each cycle of delta(value)
                        cycle = GetNumberOfCycle(position);
                        virtualPos = position - (cycle * (last.Position - first.Position));
                        return (Evaluate2(virtualPos, interpolant) + cycle * (last.Value - first.Value));

                    case CurveLoopType.Oscillate:
                        //go back on curve from end and target start 
                        // start-> end / end -> start
                        cycle = GetNumberOfCycle(position);
                        virtualPos = position - (cycle * (last.Position - first.Position));
                        if (0 == cycle % 2f)//if pair
                            virtualPos = position - (cycle * (last.Position - first.Position));
                        else
                            virtualPos = last.Position - position + first.Position + (cycle * (last.Position - first.Position));
                        return Evaluate2(virtualPos, interpolant);
                }
            }

            //in curve
            return Evaluate2(position, interpolant);
        }

        /// <summary>
        /// Computes tangents for all keys in the collection.
        /// </summary>
        /// <param name="tangentType">The tangent type for both in and out.</param>
        public void ComputeTangents (CurveTangent tangentType)
		{
		    ComputeTangents(tangentType, tangentType);
		}
		
        /// <summary>
        /// Computes tangents for all keys in the collection.
        /// </summary>
        /// <param name="tangentInType">The tangent in-type. <see cref="CurveKey.TangentIn"/> for more details.</param>
        /// <param name="tangentOutType">The tangent out-type. <see cref="CurveKey.TangentOut"/> for more details.</param>
		public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
		{
            for (var i = 0; i < Keys.Count; ++i)
            {
                ComputeTangent(i, tangentInType, tangentOutType);
            }
		}

        /// <summary>
        /// Computes tangent for the specific key in the collection.
        /// </summary>
        /// <param name="keyIndex">The index of a key in the collection.</param>
        /// <param name="tangentType">The tangent type for both in and out.</param>
        public void ComputeTangent(int keyIndex, CurveTangent tangentType)
        {
            ComputeTangent(keyIndex, tangentType, tangentType);
        }

        /// <summary>
        /// Computes tangent for the specific key in the collection.
        /// </summary>
        /// <param name="keyIndex">The index of key in the collection.</param>
        /// <param name="tangentInType">The tangent in-type. <see cref="CurveKey.TangentIn"/> for more details.</param>
        /// <param name="tangentOutType">The tangent out-type. <see cref="CurveKey.TangentOut"/> for more details.</param>
        public void ComputeTangent(int keyIndex, CurveTangent tangentInType, CurveTangent tangentOutType)
        {
            // See http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.curvetangent.aspx

            var key = _keys[keyIndex];

            float p0, p, p1;
            p0 = p = p1 = key.Position;

            float v0, v, v1;
            v0 = v = v1 = key.Value;

            if ( keyIndex > 0 )
            {
                p0 = _keys[keyIndex - 1].Position;
                v0 = _keys[keyIndex - 1].Value;
            }

            if (keyIndex < _keys.Count-1)
            {
                p1 = _keys[keyIndex + 1].Position;
                v1 = _keys[keyIndex + 1].Value;
            }

            switch (tangentInType)
            {
                case CurveTangent.Flat:
                    key.TangentIn = 0;
                    break;
                case CurveTangent.Linear:
                    key.TangentIn = v - v0;
                    break;
                case CurveTangent.Smooth:
                    var pn = p1 - p0;
                    if (Math.Abs(pn) < float.Epsilon)
                        key.TangentIn = 0;
                    else
                        key.TangentIn = (v1 - v0) * ((p - p0) / pn);
                    break;
            }

            switch (tangentOutType)
            {
                case CurveTangent.Flat:
                    key.TangentOut = 0;
                    break;
                case CurveTangent.Linear:
                    key.TangentOut = v1 - v;
                    break;
                case CurveTangent.Smooth:
                    var pn = p1 - p0;
                    if (Math.Abs(pn) < float.Epsilon)
                        key.TangentOut = 0;
                    else
                        key.TangentOut = (v1 - v0) * ((p1 - p) / pn);
                    break;
            }
        }

        public float EvaluateCurvature(float position)
        {
            return Math.Abs(EvaluateSignedCurvature(position));
        }

        public float EvaluateSignedCurvature(float position)
        {
            float dy = Evaluate(position, DefaultInterpolantDerivative);
            float ddy = Evaluate(position, DefaultInterpolantDerivative2);
            return ddy / (float)Math.Pow(1 + dy * dy, 3 / 2f);
        }

        public float EvaluateSignedCurvatureDerivative(float position)
        {
            float dy = Evaluate(position, DefaultInterpolantDerivative);
            float ddy = Evaluate(position, DefaultInterpolantDerivative2);
            return ddy / (float)Math.Pow(1 + dy * dy, 3 / 2f);
        }

        public float ComputeLocalMaxCurvaturePosition(int keyIndex)
        {
            CurveKey key1 = _keys[keyIndex];
            CurveKey key2 = _keys[keyIndex + 1];
            float num = (3 * key1.Value + 2 * key1.TangentOut - 3 * key2.Value + key2.TangentIn);
            float denom = 3 * (2 * key1.Value + key1.TangentOut - 2 * key2.Value + key2.TangentIn);
            if (denom == 0)
                return key2.Position;
            float interpolant = num / denom;
            return key1.Position + (key2.Position - key1.Position) * interpolant;
        }

        public float ComputeMaxCurvature(int keyIndex)
        {
            float left = EvaluateCurvature(Keys[keyIndex].Position);
            if (keyIndex >= _keys.Count - 1)
                return left;
            float localMaxPos = ComputeLocalMaxCurvaturePosition(keyIndex);
            float localMax = EvaluateCurvature(localMaxPos);
            float right = EvaluateCurvature(Keys[keyIndex+1].Position);
            float max = Math.Max(localMax, left);
            max = Math.Max(max, right);
            return max;
        }

        public Vector2 EvaluateNormal(float position)
        {
            float dy = Evaluate(position, DefaultInterpolantDerivative);
            Vector2 normal = new Vector2(1, dy);
            normal.Normalize();
            return normal;
        }

        #endregion

        #region Private Methods

        private int GetNumberOfCycle(float position)
        {
            float cycle = (position - _keys[0].Position) / (_keys[_keys.Count - 1].Position - _keys[0].Position);
            if (cycle < 0f)
                cycle--;
            return (int)cycle;
        }

        private float Evaluate2(float position, Func<float, CurveKey, CurveKey, float> interpolant)
        {
            //only for position in curve
            int nextIndex = this._keys.IndexAtPosition(position);
            if (nextIndex < 0)
                nextIndex = ~nextIndex;
            nextIndex = Math.Max(nextIndex, 1);
            CurveKey prev = _keys[nextIndex - 1];
            CurveKey next = _keys[nextIndex];
            if (prev.Continuity == CurveContinuity.Step)
            {
                if (position >= 1f)
                {
                    return next.Value;
                }
                return prev.Value;
            }
            float t = 0;
            float length = next.Position - prev.Position;
            if (length > 0)
                t = (position - prev.Position) / (length);//t = position mapped to [0,1]
            
            return interpolant(t, prev, next);
        }

        public float DefaultInterpolant(float t, CurveKey prev, CurveKey next)
        {
            //After a lot of search on internet I have found all about spline function
            // and bezier (phi'sss ancien) but finaly use hermite curve 
            //http://en.wikipedia.org/wiki/Cubic_Hermite_spline
            //P(t) = (2*t^3 - 3t^2 + 1)*P0 + (t^3 - 2t^2 + t)m0 + (-2t^3 + 3t^2)P1 + (t^3-t^2)m1
            //with P0.value = prev.value , m0 = prev.tangentOut, P1= next.value, m1 = next.TangentIn
            float ts = t * t;
            float tss = ts * t;
            return (2 * tss - 3 * ts + 1f) * prev.Value + (tss - 2 * ts + t) * prev.TangentOut + (3 * ts - 2 * tss) * next.Value + (tss - ts) * next.TangentIn;
        }

        static public float DefaultInterpolantDerivative(float t, CurveKey prev, CurveKey next)
        {
            float ts = t * t;
            return (6 * ts - 6 * t) * prev.Value + (3 * ts - 4 * t + 1) * prev.TangentOut + (6 * t - 6 * ts) * next.Value + (3 * ts - 2 * t) * next.TangentIn;
        }

        static public float DefaultInterpolantDerivative2(float t, CurveKey prev, CurveKey next)
        {
            return (12 * t - 6) * prev.Value + (6 * t - 4) * prev.TangentOut + (6 - 12 * t) * next.Value + (6 * t - 2) * next.TangentIn;
        }

        public void ComputeNextVectors()
        {
            Vector2 nextVec = new Vector2();
            for (int i = 0; i < _keys.Count; i++)
            {
                if (i == _keys.Count - 1)
                {
                    nextVec.X = 1;
                    nextVec.Y = 0;
                }
                else
                {
                    nextVec.X = _keys[i + 1].Position - _keys[i].Position;
                    nextVec.Y = _keys[i + 1].Value - _keys[i].Value;
                }
                _keys[i].NextVector = nextVec;
            }
        }

        #endregion
    }

}
