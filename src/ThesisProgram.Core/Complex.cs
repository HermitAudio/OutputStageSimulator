namespace ThesisProgram.Core;

/// <summary>
/// Port of module `complexmath` (thesis appendix D.1, p.114-116).
/// </summary>
public readonly struct Complex : IEquatable<Complex>
{
    public static readonly Complex Zero = new(0.0, 0.0);

    public double Re { get; }
    public double Im { get; }

    public Complex(double re, double im)
    {
        Re = re;
        Im = im;
    }

    public static Complex Add(Complex a, Complex b) => new(a.Re + b.Re, a.Im + b.Im);

    public static Complex Sub(Complex a, Complex b) => new(a.Re - b.Re, a.Im - b.Im);

    public static Complex Mul(Complex a, Complex b) =>
        new(a.Re * b.Re - a.Im * b.Im, a.Re * b.Im + a.Im * b.Re);

    /// <summary>Pascal `dvd` calls halt(-5) on a zero denominator; ported as an exception.</summary>
    public static Complex Div(Complex a, Complex b)
    {
        var denom = b.Re * b.Re + b.Im * b.Im;
        if (denom == 0.0)
        {
            throw new DivideByZeroException("Complex division by zero (Pascal: halt(-5)).");
        }

        return new Complex(
            (b.Re * a.Re + b.Im * a.Im) / denom,
            (b.Re * a.Im - b.Im * a.Re) / denom);
    }

    public static Complex Conj(Complex a) => new(a.Re, -a.Im);

    public static double Mag(Complex a) => Math.Sqrt(a.Re * a.Re + a.Im * a.Im);

    /// <summary>Phase angle in radians, in (-pi, pi], as returned by atan2(Im, Re).</summary>
    public static double Phase(Complex a) => Math.Atan2(a.Im, a.Re);

    public static Complex ScMul(double scale, Complex a) => new(scale * a.Re, scale * a.Im);

    public static Complex PolarToRect(double mag, double phase) =>
        new(mag * Math.Cos(phase), mag * Math.Sin(phase));

    public static Complex operator +(Complex a, Complex b) => Add(a, b);
    public static Complex operator -(Complex a, Complex b) => Sub(a, b);
    public static Complex operator *(Complex a, Complex b) => Mul(a, b);
    public static Complex operator /(Complex a, Complex b) => Div(a, b);

    public bool Equals(Complex other) => Re.Equals(other.Re) && Im.Equals(other.Im);
    public override bool Equals(object? obj) => obj is Complex other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Re, Im);
    public override string ToString() => $"({Re}, {Im})";
}
