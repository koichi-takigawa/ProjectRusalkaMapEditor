using System;

namespace JL.Tactics
{
    // <summary>•ûŒü</summary>
    internal enum Direction
    {
        Direction_Invalid = 0,

        /// <summary>01•ûŒü(q+r-)</summary>
        Direction_01 = 30,

        /// <summary>03•ûŒü(q+)</summary>
        Direction_03 = 90,

        /// <summary>05•ûŒü(r+)</summary>
        Direction_05 = 150,

        /// <summary>07•ûŒü(q-r+)</summary>
        Direction_07 = 210,

        /// <summary>09•ûŒü(q-)</summary>
        Direction_09 = 270,

        /// <summary>11•ûŒü(r-)</summary>
        Direction_11 = 330,
    }

    // <summary>˜ZŠpŒ`ˆÊ’u‚Ì·•ª</summary>
    internal class Hex2Offset
    {
        public Hex2Offset(int q, int r)
        {
            this.Q = q;
            this.R = r;
        }

        /// <summary>
        /// q²’lBx²‘Š“–‚Ì²B
        /// </summary>
        public int Q { get; set; }

        /// <summary>
        /// r²’lB11(-) - 5(+) •ûŒü‚Ì²‚ÅAUnity‚ÌZ²‚Æ‚Íƒvƒ‰ƒXƒ}ƒCƒiƒX‚ª‹tB
        /// </summary>
        public int R { get; set; }

        /// <summary>
        /// s²’lBreadonly
        /// </summary>
        public int S { get { return -(Q + R); } }

        /// <summary>
        /// ƒXƒeƒbƒv”‚ğ•Ô‚·B
        /// </summary>
        public int Step
        {
            get
            {
                return (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;
            }
        }
    }

    // <summary>˜ZŠpŒ`ˆÊ’u</summary>
    internal class Hex2
    {
        const float SQRT_3 = 1.7320508f;

        public int Q { get; set; }
        public int R { get; set; }
        public int S { get { return -Q - R; } }

        public Hex2() { }

        public Hex2(int q, int r)
        {
            this.Q = q;
            this.R = r;
        }

        public static bool operator ==(Hex2 a, Hex2 b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Q == b.Q && a.R == b.R;
        }

        public static bool operator !=(Hex2 a, Hex2 b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is Hex2 hex)
            {
                return Q == hex.Q && R == hex.R;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (1000 + Q) * 1000000 + (1000 + R) * 1000;
        }

        /// <summary>
        /// ˜ZŠpŒ`‚ÉŠOÚ‚·‚é‰~‚Ì”¼Œa‚ª‚P‚Å‚ ‚é’PˆÊŒn‚Å‚ÌHex‚ÌˆÊ’u‚ğ‹‚ß‚éB
        /// </summary>
        /// <param name="x">³‹K‰»À•WX</param>
        /// <param name="y">³‹K‰»À•WY</param>
        /// <returns></returns>
        public static Hex2 ToHex(float x, float y)
        {
#if false
            float fracQ = 2.0F / 3.0F * x;
            float fracR = -1.0F / 3.0F * x + SQRT_3 / 3.0F * y;
#else
            float fracQ = (SQRT_3 / 3 * x + 1.0F / 3 * y);
            float fracR = -(2.0F / 3 * y);
#endif
            float fracS = -fracQ - fracR;

            int q = (int)Math.Round(fracQ);
            int r = (int)Math.Round(fracR);
            int s = (int)Math.Round(fracS);

            float qDiff = Math.Abs(q - fracQ);
            float rDiff = Math.Abs(r - fracR);
            float sDiff = Math.Abs(s - fracS);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                q = -r - s;
            }
            else if (rDiff > sDiff)
            {
                r = -q - s;
            }
            else
            {
                s = -q - r;
            }

            return new Hex2(q, r);
        }

        /// <summary>
        /// ˜ZŠpŒ`‚ÉŠOÚ‚·‚é‰~‚Ì”¼Œa‚ª‚P‚Å‚ ‚é’PˆÊŒn‚Å‚ÌHex‚ÌˆÊ’u‚ğ‹‚ß‚éB
        /// </summary>
        /// <param name="x">³‹K‰»À•WX</param>
        /// <param name="y">³‹K‰»À•WY</param>
        public void ToPointFloat(out float x, out float y)
        {
#if false
            x = q * 3.0F / 2.0F;
            y = SQRT_3 * (q / 2.0F + r);
#else
            x = SQRT_3 * (Q + R / 2.0F);
            y = -R * 3.0F / 2.0F;
#endif
        }

        /// <summary>
        /// ˜ZŠpŒ`ŠÔ‚Ì‹——£‚ğ‚à‚Æ‚ß‚éB—×‚Ìƒ}ƒX‚Í‚P
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public int StepTo(Hex2 hex)
        {
            return (this - hex).Step;
        }

        // <summary>w’è•ûŒü‚ÉˆÚ“®‚µ‚½Hex‚ğ•Ô‚·B</summary>
        public Hex2 MoveTo(Direction dir)
        {
            return dir switch
            {
                Direction.Direction_01 => new Hex2(Q + 1, R - 1),
                Direction.Direction_03 => new Hex2(Q + 1, R),
                Direction.Direction_05 => new Hex2(Q, R + 1),
                Direction.Direction_07 => new Hex2(Q - 1, R + 1),
                Direction.Direction_09 => new Hex2(Q - 1, R),
                Direction.Direction_11 => new Hex2(Q, R - 1),
                _ => this,
            };
        }

        // ‰ÁZ
        public static Hex2 operator +(Hex2 lhs, Hex2Offset rhs)
        {
            return new Hex2(lhs.Q + rhs.Q, lhs.R + rhs.R);
        }

        // Œ¸Z
        public static Hex2Offset operator -(Hex2 lhs, Hex2 rhs)
        {
            return new Hex2Offset(lhs.Q - rhs.Q, lhs.R - rhs.R);
        }
    }

    // <summary>˜ZŠpŒ`ˆÊ’u{‚‚³‚Ì·•ª</summary>
    internal class Hex3Offset
    {
        public int Q { get; set; }
        public int R { get; set; }
        public int S { get { return -Q - R; } }
        public int H { get; set; }

        public Hex3Offset(int q, int r, int h)
        {
            this.Q = q;
            this.R = r;
            this.H = h;
        }

        public override int GetHashCode()
        {
            return (1000 + Q) * 1000000 + (1000 + R) * 1000 + (1000 + H);
        }
    }

    // <summary>˜ZŠpŒ`ˆÊ’u{‚‚³</summary>
    internal class Hex3 : Hex2
    {
        public static Hex3 Zero = new(0, 0, 0);

        public int H { get; set; }

        public Hex3() { }

        public Hex3(int q, int r, int h)
        {
            this.Q = q;
            this.R = r;
            this.H = h;
        }

        public override int GetHashCode()
        {
            return (1000 + Q) * 1000000 + (1000 + R) * 1000 + (1000 + H);
        }

        public override bool Equals(object obj)
        {
            if (obj is Hex3 other)
            {
                return this.Q == other.Q && this.R == other.R && this.H == other.H;
            }
            return false;
        }

        public static Hex3 operator +(Hex3 a, Hex3Offset b)
        {
            return new Hex3(a.Q + b.Q, a.R + b.R, a.H + b.H);
        }

        public static bool operator ==(Hex3 a, Hex3 b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Q == b.Q && a.R == b.R && a.H == b.H;
        }

        public static bool operator !=(Hex3 a, Hex3 b)
        {
            return !(a == b);
        }
    }
}
