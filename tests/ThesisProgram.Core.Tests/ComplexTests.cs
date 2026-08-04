using ThesisProgram.Core;

namespace ThesisProgram.Core.Tests;

[TestFixture]
public class ComplexTests
{
    [Test]
    public void Add_SumsComponents()
    {
        var result = Complex.Add(new Complex(1, 2), new Complex(3, 4));
        Assert.That(result.Re, Is.EqualTo(4));
        Assert.That(result.Im, Is.EqualTo(6));
    }

    [Test]
    public void Sub_SubtractsComponents()
    {
        var result = Complex.Sub(new Complex(3, 4), new Complex(1, 1));
        Assert.That(result.Re, Is.EqualTo(2));
        Assert.That(result.Im, Is.EqualTo(3));
    }

    [Test]
    public void Mul_MatchesComplexMultiplication()
    {
        // (2+3i)*(4+5i) = 8+10i+12i+15i^2 = (8-15) + (10+12)i = -7+22i
        var result = Complex.Mul(new Complex(2, 3), new Complex(4, 5));
        Assert.That(result.Re, Is.EqualTo(-7));
        Assert.That(result.Im, Is.EqualTo(22));
    }

    [Test]
    public void Div_IsInverseOfMul()
    {
        var a = new Complex(2, 3);
        var b = new Complex(4, -1);
        var product = Complex.Mul(a, b);
        var recovered = Complex.Div(product, b);

        Assert.That(recovered.Re, Is.EqualTo(a.Re).Within(1e-12));
        Assert.That(recovered.Im, Is.EqualTo(a.Im).Within(1e-12));
    }

    [Test]
    public void Div_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Complex.Div(new Complex(1, 1), Complex.Zero));
    }

    [Test]
    public void Conj_NegatesImaginaryPart()
    {
        var result = Complex.Conj(new Complex(3, 4));
        Assert.That(result.Re, Is.EqualTo(3));
        Assert.That(result.Im, Is.EqualTo(-4));
    }

    [Test]
    public void Mag_ComputesEuclideanNorm()
    {
        Assert.That(Complex.Mag(new Complex(3, 4)), Is.EqualTo(5).Within(1e-12));
    }

    [Test]
    public void ScMul_ScalesBothComponents()
    {
        var result = Complex.ScMul(2.5, new Complex(2, -4));
        Assert.That(result.Re, Is.EqualTo(5));
        Assert.That(result.Im, Is.EqualTo(-10));
    }

    [Test]
    public void PolarToRect_MatchesTrigIdentity()
    {
        var result = Complex.PolarToRect(2.0, Math.PI / 2);
        Assert.That(result.Re, Is.EqualTo(0).Within(1e-12));
        Assert.That(result.Im, Is.EqualTo(2).Within(1e-12));
    }

    [Test]
    public void Operators_MatchStaticMethods()
    {
        var a = new Complex(1, 2);
        var b = new Complex(3, 4);

        Assert.That((a + b).Equals(Complex.Add(a, b)));
        Assert.That((a - b).Equals(Complex.Sub(a, b)));
        Assert.That((a * b).Equals(Complex.Mul(a, b)));
        Assert.That((a / b).Equals(Complex.Div(a, b)));
    }
}
