using System.Text;
using FbxDataExtractor;

namespace FbxImporter.Tests;

public class UnicodeImportTests : IDisposable
{
    private const string MeshName = "网格_メッシュ_ü";
    private const string BoneName = "左手_Ł_é";

    // One triangle, skinned with full weight to one bone. ASCII FBX 7.4; the names are UTF-8, as the FBX SDK expects.
    private const string Fbx = """
        ; FBX 7.4.0 project file
        FBXHeaderExtension:  {
        	FBXHeaderVersion: 1003
        	FBXVersion: 7400
        }
        GlobalSettings:  {
        	Version: 1000
        	Properties70:  {
        		P: "UpAxis", "int", "Integer", "",1
        		P: "UpAxisSign", "int", "Integer", "",1
        		P: "FrontAxis", "int", "Integer", "",2
        		P: "FrontAxisSign", "int", "Integer", "",1
        		P: "CoordAxis", "int", "Integer", "",0
        		P: "CoordAxisSign", "int", "Integer", "",1
        		P: "UnitScaleFactor", "double", "Number", "",1
        	}
        }
        Definitions:  {
        	Version: 100
        	Count: 7
        	ObjectType: "Model" {
        		Count: 2
        	}
        	ObjectType: "Geometry" {
        		Count: 1
        	}
        	ObjectType: "NodeAttribute" {
        		Count: 1
        	}
        	ObjectType: "Deformer" {
        		Count: 2
        	}
        }
        Objects:  {
        	Geometry: 1000, "Geometry::MESH", "Mesh" {
        		Vertices: *9 {
        			a: 0,0,0,1,0,0,0,1,0
        		}
        		PolygonVertexIndex: *3 {
        			a: 0,1,-3
        		}
        		GeometryVersion: 124
        		LayerElementNormal: 0 {
        			Version: 101
        			Name: ""
        			MappingInformationType: "ByPolygonVertex"
        			ReferenceInformationType: "Direct"
        			Normals: *9 {
        				a: 0,0,1,0,0,1,0,0,1
        			}
        		}
        		LayerElementUV: 0 {
        			Version: 101
        			Name: "UVMap"
        			MappingInformationType: "ByPolygonVertex"
        			ReferenceInformationType: "IndexToDirect"
        			UV: *6 {
        				a: 0,0,1,0,0,1
        			}
        			UVIndex: *3 {
        				a: 0,1,2
        			}
        		}
        		Layer: 0 {
        			Version: 100
        			LayerElement:  {
        				Type: "LayerElementNormal"
        				TypedIndex: 0
        			}
        			LayerElement:  {
        				Type: "LayerElementUV"
        				TypedIndex: 0
        			}
        		}
        	}
        	Model: 2000, "Model::MESHNAME", "Mesh" {
        		Version: 232
        	}
        	NodeAttribute: 3000, "NodeAttribute::BONENAME", "LimbNode" {
        		TypeFlags: "Skeleton"
        	}
        	Model: 4000, "Model::BONENAME", "LimbNode" {
        		Version: 232
        	}
        	Deformer: 5000, "Deformer::", "Skin" {
        		Version: 101
        		Link_DeformAcuracy: 50
        	}
        	Deformer: 6000, "SubDeformer::", "Cluster" {
        		Version: 100
        		UserData: "", ""
        		Indexes: *3 {
        			a: 0,1,2
        		}
        		Weights: *3 {
        			a: 1,1,1
        		}
        		Transform: *16 {
        			a: 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1
        		}
        		TransformLink: *16 {
        			a: 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1
        		}
        	}
        }
        Connections:  {
        	C: "OO",2000,0
        	C: "OO",1000,2000
        	C: "OO",4000,0
        	C: "OO",3000,4000
        	C: "OO",5000,1000
        	C: "OO",6000,5000
        	C: "OO",4000,6000
        }
        """;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "テスト_测试_ěšč_" + Guid.NewGuid().ToString("N"));

    public UnicodeImportTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, true);

    private string WriteFbx()
    {
        string path = Path.Combine(_folder, "模型_ü.fbx");
        string text = Fbx.Replace("MESHNAME", MeshName).Replace("BONENAME", BoneName);
        File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    [Fact]
    public void ImportReadsAUnicodePathAndKeepsUnicodeNames()
    {
        List<FbxMeshData> meshes = FbxMeshData.Import(WriteFbx());

        FbxMeshData mesh = Assert.Single(meshes);
        Assert.Equal(MeshName, mesh.Name);
        Assert.NotEmpty(mesh.VertexData);
        Assert.All(mesh.VertexData, v => Assert.Equal(new[] { BoneName }, v.BoneNames));
    }

    [Fact]
    public void ImportOfAMissingFileSaysFileNotFound()
    {
        string path = Path.Combine(_folder, "不存在_ü.fbx");

        var e = Assert.Throws<FileNotFoundException>(() => FbxMeshData.Import(path));
        Assert.Equal(path, e.FileName);
    }

    [Fact]
    public void ImportOfAFileTheSdkCannotReadNamesTheFile()
    {
        string path = Path.Combine(_folder, "坏的_ü.fbx");
        File.WriteAllBytes(path, new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0x00, 0x00 });

        var e = Assert.Throws<InvalidDataException>(() => FbxMeshData.Import(path));
        Assert.StartsWith("Could not open 坏的_ü.fbx: ", e.Message);
    }
}
