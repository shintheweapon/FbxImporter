# FbxImporter

Tool for importing fbx meshes into flver model files. Works with Dark Souls 3, Sekiro and Elden Ring.
Ensures the correct vertex buffer layouts are used for all materials and supports multiple UV layers.

Special thanks to TKGP for Soulsformats and Meowmaritus for SoulsAssetPipeline as well as Amir for his help with testing.

## ER Tangents (this fork)

The mesh import options have an **ER Tangents** check box. It starts ticked for Elden Ring FLVERs and unticked for
the other games, and keeps the last choice made for each game.

Elden Ring's own models store each tangent along +V of their UVs, with W = +1 or -1 so that W * (normal x tangent)
points along +U. Normal maps are made for that frame. The FBX SDK's tangents point along +U, so with them a normal
map is read turned or with its channels swapped, depending on the Transform.

When ticked, the importer ignores the tangents in the FBX file and builds them from the mesh after the Transform:
for each vertex it adds up the +V and +U directions of the triangles around it, weighted by area, and makes the
+V sum perpendicular to the normal. This works for every Transform preset and for FBX files with or without their
own tangents. Unticked, the tangents are as in the upstream FbxImporter.

The convention was measured and checked in game on Elden Ring only; Dark Souls 3 and Sekiro were not checked.

## Unicode text and error messages (this fork)

FBX files open from any folder and file name, in any language. Mesh and bone names are read as the UTF-8 text the
FBX SDK returns, so they keep their characters and match FLVER bone names written in the same characters.

Every failed open, import, add or save writes one line to the log, and the app stays open:
- a missing file: "File not found: " and its path;
- a file that cannot be read or written (an FBX the FBX SDK rejects, a FLVER of the wrong kind, a read-only or
  locked file): the file and the reason;
- anything else is a bug, and its line carries the full technical detail for a report.

A Save As that fails leaves Save writing to the file opened or saved before.
