using System.Numerics;
using FbxImporter.Util;

namespace FbxImporter.Tests;

public class TangentBuilderTests
{
    // A unit quad in the XY plane facing +Z: position = u * X + v * Y, so +U is +X and +V is +Y.
    // With mirrorU the stored U runs the other way (u' = 1 - u), so +U becomes -X; +V stays +Y.
    private static (List<Vector3> P, List<Vector3> N, List<Vector2> UV, List<int> I) Quad(bool mirrorU)
    {
        var corners = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        var p = corners.Select(c => new Vector3(c.X, c.Y, 0)).ToList();
        var n = corners.Select(_ => Vector3.UnitZ).ToList();
        var uv = corners.Select(c => new Vector2(mirrorU ? 1 - c.X : c.X, c.Y)).ToList();
        return (p, n, uv, new List<int> { 0, 1, 2, 0, 2, 3 });
    }

    public static TheoryData<string, bool> Cases()
    {
        var data = new TheoryData<string, bool>();
        foreach (string t in new[] { "identity", "mirrorZ", "swapYZ" })
        foreach (bool m in new[] { false, true })
            data.Add(t, m);
        return data;
    }

    private static Matrix4x4 TransformOf(string name) => name switch
    {
        "mirrorZ" => Matrix4x4.Identity with { M33 = -1 },
        "swapYZ" => Matrix4x4.Identity with { M22 = 0, M23 = 1, M32 = 1, M33 = 0 },
        _ => Matrix4x4.Identity,
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void TangentPointsAlongVAndWFollowsU(string transformName, bool mirrorU)
    {
        Matrix4x4 m = TransformOf(transformName);
        Matrix4x4 normalMatrix = MatrixUtils.Cofactor3x3(m) * Math.Sign(m.GetDeterminant());
        var (p, n, uv, idx) = Quad(mirrorU);
        var positions = p.Select(x => Vector3.Transform(x, m)).ToList();
        var normals = n.Select(x => Vector3.Normalize(Vector3.Transform(x, normalMatrix))).ToList();
        Vector3 vDir = Vector3.TransformNormal(Vector3.UnitY, m);
        Vector3 uDir = Vector3.TransformNormal(mirrorU ? -Vector3.UnitX : Vector3.UnitX, m);

        var tangents = TangentBuilder.Build(positions, normals, uv, idx, new Vector4[4]);

        for (int i = 0; i < 4; i++)
        {
            var t = new Vector3(tangents[i].X, tangents[i].Y, tangents[i].Z);
            Assert.Equal(1f, t.Length(), 4);
            Assert.True(Vector3.Dot(t, Vector3.Normalize(vDir)) > 0.999f, $"corner {i}: tangent not along +V");
            Assert.Equal(0f, Vector3.Dot(t, normals[i]), 4);
            float expectedW = Vector3.Dot(Vector3.Cross(normals[i], t), uDir) > 0 ? 1 : -1;
            Assert.Equal(expectedW, tangents[i].W);
        }
    }

    [Fact]
    public void MirroredUHalfGetsTheOppositeW()
    {
        var (p, n, uv, idx) = Quad(false);
        var (_, _, uvMirrored, _) = Quad(true);
        float w = TangentBuilder.Build(p, n, uv, idx, new Vector4[4])[0].W;
        float wMirrored = TangentBuilder.Build(p, n, uvMirrored, idx, new Vector4[4])[0].W;
        Assert.Equal(-w, wMirrored);
    }

    [Fact]
    public void ReversedWindingGivesTheSameTangents()
    {
        var (p, n, uv, idx) = Quad(true);
        var reversed = new List<int> { 0, 2, 1, 0, 3, 2 };
        var a = TangentBuilder.Build(p, n, uv, idx, new Vector4[4]);
        var b = TangentBuilder.Build(p, n, uv, reversed, new Vector4[4]);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(a[i].W, b[i].W);
            Assert.True(Vector4.Distance(a[i], b[i]) < 1e-5f);
        }
    }

    [Fact]
    public void ScaledTransformStillGivesUnitTangentAndUnitW()
    {
        Matrix4x4 m = (Matrix4x4.Identity with { M33 = -1 }) * 0.01f;
        var (p, n, uv, idx) = Quad(false);
        var positions = p.Select(x => Vector3.Transform(x, m)).ToList();
        var tangents = TangentBuilder.Build(positions, n.Select(x => -x).ToList(), uv, idx, new Vector4[4]);
        foreach (Vector4 t in tangents)
        {
            Assert.Equal(1f, new Vector3(t.X, t.Y, t.Z).Length(), 4);
            Assert.True(MathF.Abs(t.W) == 1f);
        }
    }

    [Fact]
    public void VertexWithoutUvAreaKeepsFallbackWithUnitW()
    {
        var (p, n, _, idx) = Quad(false);
        var flatUv = Enumerable.Repeat(new Vector2(0.5f, 0.5f), 4).ToList();
        var fallback = Enumerable.Repeat(new Vector4(1, 0, 0, -0.01f), 4).ToList();
        var tangents = TangentBuilder.Build(p, n, flatUv, idx, fallback);
        Assert.All(tangents, t => Assert.Equal(new Vector4(1, 0, 0, -1), t));
    }

    [Fact]
    public void ClothWKeepsTheDirectionAndSetsWToMinusOne()
    {
        var tangents = new List<Vector4> { new(0, 1, 0, 1), new(1, 0, 0, -1), new(0.6f, 0.8f, 0, 1) };
        List<Vector4> cloth = TangentBuilder.WithClothW(tangents);
        Assert.Equal(tangents.Count, cloth.Count);
        for (int i = 0; i < tangents.Count; i++)
        {
            Assert.Equal(new Vector3(tangents[i].X, tangents[i].Y, tangents[i].Z),
                new Vector3(cloth[i].X, cloth[i].Y, cloth[i].Z));
            Assert.Equal(-1, cloth[i].W);
        }
    }
}
