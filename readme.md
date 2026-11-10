# FbxImporter

Tool for importing fbx meshes into flver model files. Works with Dark Souls 3, Sekiro and Elden Ring.
Ensures the correct vertex buffer layouts are used for all materials and supports multiple UV layers.

Special thanks to TKGP for Soulsformats and Meowmaritus for SoulsAssetPipeline as well as Amir for his help with testing.

## ER Tangents (this fork)

The mesh import options have an **ER Tangents** check box, ticked by default.

Elden Ring's own models store each tangent along +V of their UVs, with W = +1 or -1 so that W * (normal x tangent)
points along +U. Normal maps are made for that frame. The FBX SDK's tangents point along +U, so with them a normal
map is read turned or with its channels swapped, depending on the Transform.

When ticked, the importer ignores the tangents in the FBX file and builds them from the mesh after the Transform:
for each vertex it adds up the +V and +U directions of the triangles around it, weighted by area, and makes the
+V sum perpendicular to the normal. This works for every Transform preset and for FBX files with or without their
own tangents. Unticked, the tangents are as in the upstream FbxImporter.

The convention was measured and checked in game on Elden Ring only; Dark Souls 3 and Sekiro were not checked.
